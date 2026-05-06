using System.Collections.Generic;
using UnityEngine;

namespace GameMain.RunTime
{
    public class BeeRandomData : MonoBehaviour
    {
        private static List<Vector2> usedValues = new List<Vector2>();

        public Vector2 myUniqueVar;
        public float minDifference = 0.2f; // 强制最小差异

        void Awake()
        {
            myUniqueVar = GenerateUniqueVector2();
        }

        Vector2 GenerateUniqueVector2()
        {
            int attempts = 0;
            while (attempts < 100)
            {
                // 在指定范围内生成随机值
                Vector2 candidate = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));

                bool isUnique = true;
                foreach (var val in usedValues)
                {
                    // 检查是否与已有的变量太接近
                    if (Vector2.Distance(candidate, val) < minDifference)
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique)
                {
                    usedValues.Add(candidate);
                    return candidate;
                }
                attempts++;
            }
            return Vector2.zero; // 备选方案
        }

        // 注意：如果是切关或者重启，需要手动清理静态列表
        void OnDestroy()
        {
            usedValues.Clear();
        }
    }
}
