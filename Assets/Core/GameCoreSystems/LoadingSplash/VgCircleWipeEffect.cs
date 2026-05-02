using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [RequireComponent(typeof(Image))]
    public class VgCircleWipeEffect : MonoProgressable
    {
        public void SetCenter(Vector2 uvCenter)
        {
            EnsureMaterial();
            m_Material.SetVector(CenterId, new Vector4(uvCenter.x, uvCenter.y, 0f, 0f));
        }

        public float GetRadius()
        {
            EnsureMaterial();
            return m_Material.GetFloat(RadiusId);
        }

        public void SetRadius(float radius)
        {
            EnsureMaterial();
            m_Material.SetFloat(RadiusId, Mathf.Max(0f, radius));
        }

        public override void Show()
        {
            EnsureMaterial();
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        public override void UpdateProgress(float progress) { }

        private void EnsureMaterial()
        {
            if (m_Material != null)
                return;

            var image = GetComponent<Image>();
            m_Material = new Material(image.material);
            image.material = m_Material;
        }

        private void OnDestroy()
        {
            if (m_Material != null)
                Destroy(m_Material);
        }

        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        private Material m_Material;
    }
}
