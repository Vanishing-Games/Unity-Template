using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using UnityEngine;

namespace Core
{
    public class InstantiateGoCommand : ICommand<GameObject>, IUndoableCommand<bool>
    {
        public InstantiateGoCommand(GameObject prefab)
            : this(prefab, Vector3.zero, Quaternion.identity, Vector3.one) { }

        public InstantiateGoCommand(GameObject prefab, Vector3 position)
            : this(prefab, position, Quaternion.identity, Vector3.one) { }

        public InstantiateGoCommand(GameObject prefab, Vector3 position, Quaternion rotation)
            : this(prefab, position, rotation, Vector3.one) { }

        public InstantiateGoCommand(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale
        )
        {
            mPrefab = prefab;
            mPosition = position;
            mRotation = rotation;
            mScale = scale;
        }

        public GameObject Execute()
        {
            if (mPrefab == null)
            {
                Logger.LogError("InstantiateGoCommand: Prefab is null!", LogTag.Command);
                return null;
            }

            mInstance = Object.Instantiate(mPrefab, mPosition, mRotation);
            if (mInstance != null)
            {
                mInstance.transform.localScale = mScale;
            }
            return mInstance;
        }

        public bool Undo()
        {
            if (mInstance != null)
            {
                Object.Destroy(mInstance);
                return true;
            }
            return false;
        }

        private GameObject mPrefab;
        private Vector3 mPosition;
        private Quaternion mRotation;
        private Vector3 mScale;
        private GameObject mInstance;
    }
}
