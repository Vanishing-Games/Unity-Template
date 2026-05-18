namespace GameMain.RunTime
{
    public interface ICharacterEffect
    {
        void Apply(ref CharacterEffectContext ctx);
    }
}
