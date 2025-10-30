using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class VgLoadingProgressBar : MonoProgressable
    {
        public override void Hide()
        {
            gameObject.SetActive(false);
            m_Material = null;
        }

        public override void Show()
        {
            gameObject.SetActive(true);
            m_Material = GetComponent<Renderer>().material;
        }

        public override void UpdateProgress(float progress)
        {
            m_Material.SetFloat("_Fill", progress);
        }

        private Material m_Material;
    }
}
