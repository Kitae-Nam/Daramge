using UnityEngine;

namespace CHG._02_Scripts.UI
{
    public class CharacterPreview : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform[] models;
        [SerializeField] private float rotateSpeed = 40f;

        private Quaternion[] _startRotations;
        private Transform _current;

        public RenderTexture Texture => previewCamera.targetTexture;

        private void Awake()
        {
            Debug.Assert(previewCamera != null, $"previewCamera is null : {gameObject.name}");
            previewCamera.enabled = false;

            _startRotations = new Quaternion[models.Length];
            for (int i = 0; i < models.Length; i++)
            {
                _startRotations[i] = models[i].localRotation;
                models[i].gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_current != null)
                _current.Rotate(Vector3.up, rotateSpeed * Time.unscaledDeltaTime, Space.World);
        }

        public void Show(int index)
        {
            Hide();

            _current = models[index];
            _current.localRotation = _startRotations[index];
            _current.gameObject.SetActive(true);
            previewCamera.enabled = true;
        }

        public void Hide()
        {
            if (_current != null)
                _current.gameObject.SetActive(false);

            _current = null;
            previewCamera.enabled = false;
        }
    }
}