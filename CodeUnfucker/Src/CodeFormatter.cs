using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker
{
    public class CodeFormatter
    {
        private readonly FormatterConfig _config;

        public CodeFormatter()
        {
            _config = ConfigManager.GetFormatterConfig();
        }

        public string FormatCode(string sourceCode, string filePath)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var newRoot = (CompilationUnitSyntax)VisitCompilationUnit(root);

            return newRoot.NormalizeWhitespace().ToFullString();
        }

        private CompilationUnitSyntax VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var newMembers = new List<MemberDeclarationSyntax>();

            foreach (var member in node.Members)
            {
                if (member is ClassDeclarationSyntax classDecl)
                {
                    newMembers.Add(ReorganizeClass(classDecl));
                }
                else
                {
                    newMembers.Add(member);
                }
            }

            return node.WithMembers(SyntaxFactory.List(newMembers));
        }

        private ClassDeclarationSyntax ReorganizeClass(ClassDeclarationSyntax classDecl)
        {
            var memberGroups = CategorizeMethods(classDecl.Members);

            var newMembers = new List<MemberDeclarationSyntax>();

            AddRegionGroup(
                newMembers,
                _config.RegionSettings.PublicRegionName,
                memberGroups.PublicMembers
            );
            AddRegionGroup(
                newMembers,
                _config.RegionSettings.UnityLifeCycleRegionName,
                memberGroups.UnityLifeCycleMembers
            );
            AddRegionGroup(
                newMembers,
                _config.RegionSettings.ProtectedRegionName,
                memberGroups.ProtectedMembers
            );
            AddRegionGroup(
                newMembers,
                _config.RegionSettings.PrivateRegionName,
                memberGroups.PrivateMembers
            );
            AddRegionGroup(
                newMembers,
                _config.RegionSettings.NestedClassesRegionName,
                memberGroups.NestedClasses
            );
            AddRegionGroup(
                newMembers,
                _config.RegionSettings.MemberVariablesRegionName,
                memberGroups.MemberVariables
            );

            return classDecl.WithMembers(SyntaxFactory.List(newMembers));
        }

        private void AddRegionGroup(
            List<MemberDeclarationSyntax> newMembers,
            string regionName,
            List<MemberDeclarationSyntax> members
        )
        {
            if (members.Count == 0)
                return;

            // 计算大概的行数来决定是否添加Region
            int totalLines = members.Sum(m => m.GetText().Lines.Count);

            if (_config.FormatterSettings.EnableRegionGeneration && totalLines >= _config.FormatterSettings.MinLinesForRegion)
            {
                // 添加 #region 指令
                var regionDirective = SyntaxFactory
                    .RegionDirectiveTrivia(true)
                    .WithEndOfDirectiveToken(
                        SyntaxFactory
                            .Token(SyntaxKind.EndOfDirectiveToken)
                            .WithLeadingTrivia(
                                SyntaxFactory.Space,
                                SyntaxFactory.PreprocessingMessage(regionName)
                            )
                    );

                var regionTrivia = SyntaxFactory.Trivia(regionDirective);
                var leadingTrivia = SyntaxFactory.TriviaList(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Whitespace("        "),
                    regionTrivia,
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.CarriageReturnLineFeed
                );

                var firstMember = members.First().WithLeadingTrivia(leadingTrivia);
                newMembers.Add(firstMember);

                // 添加其余成员
                for (int i = 1; i < members.Count; i++)
                {
                    newMembers.Add(members[i]);
                }

                // 添加 #endregion 指令
                var endRegionDirective = SyntaxFactory.EndRegionDirectiveTrivia(true);
                var endRegionTrivia = SyntaxFactory.Trivia(endRegionDirective);
                var endRegionTriviaList = SyntaxFactory.TriviaList(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Whitespace("        "),
                    endRegionTrivia,
                    SyntaxFactory.CarriageReturnLineFeed
                );

                var lastMember = newMembers.Last().WithTrailingTrivia(endRegionTriviaList);
                newMembers[newMembers.Count - 1] = lastMember;
            }
            else
            {
                // 如果代码不够长，直接添加成员不使用Region
                newMembers.AddRange(members);
            }
        }

        private MemberGroups CategorizeMethods(SyntaxList<MemberDeclarationSyntax> members)
        {
            var groups = new MemberGroups();

            foreach (var member in members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax
                    or PropertyDeclarationSyntax:
                        groups.MemberVariables.Add(member);
                        break;

                    case ClassDeclarationSyntax
                    or StructDeclarationSyntax
                    or EnumDeclarationSyntax:
                        groups.NestedClasses.Add(member);
                        break;

                    case MethodDeclarationSyntax method:
                        CategorizeMethod(method, groups);
                        break;

                    case ConstructorDeclarationSyntax constructor:
                        CategorizeConstructor(constructor, groups);
                        break;

                    default:
                        // 其他成员按可见性分类
                        CategorizeByVisibility(member, groups);
                        break;
                }
            }

            return groups;
        }

        private void CategorizeMethod(MethodDeclarationSyntax method, MemberGroups groups)
        {
            var methodName = method.Identifier.ValueText;

            // 检查是否是Unity生命周期方法
            if (IsUnityLifeCycleMethod(methodName))
            {
                groups.UnityLifeCycleMembers.Add(method);
                return;
            }

            CategorizeByVisibility(method, groups);
        }

        private void CategorizeConstructor(
            ConstructorDeclarationSyntax constructor,
            MemberGroups groups
        )
        {
            CategorizeByVisibility(constructor, groups);
        }

        private void CategorizeByVisibility(MemberDeclarationSyntax member, MemberGroups groups)
        {
            if (IsPublic(member))
            {
                groups.PublicMembers.Add(member);
            }
            else if (IsProtected(member))
            {
                groups.ProtectedMembers.Add(member);
            }
            else
            {
                groups.PrivateMembers.Add(member);
            }
        }

        private bool IsUnityLifeCycleMethod(string methodName)
        {
            return _config.UnityLifeCycleMethods.Contains(methodName);
        }

        private bool IsPublic(MemberDeclarationSyntax member)
        {
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private bool IsProtected(MemberDeclarationSyntax member)
        {
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword));
        }

        private class MemberGroups
        {
            public List<MemberDeclarationSyntax> PublicMembers { get; } = new();
            public List<MemberDeclarationSyntax> UnityLifeCycleMembers { get; } = new();
            public List<MemberDeclarationSyntax> ProtectedMembers { get; } = new();
            public List<MemberDeclarationSyntax> PrivateMembers { get; } = new();
            public List<MemberDeclarationSyntax> NestedClasses { get; } = new();
            public List<MemberDeclarationSyntax> MemberVariables { get; } = new();
        }
    }
}
