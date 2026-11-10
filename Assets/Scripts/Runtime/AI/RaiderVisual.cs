using UnityEngine;

namespace StealthRaider.AI
{
    /// <summary>Poses for the placeholder body made of primitives: hands up, dropped gun, fallen.</summary>
    public class RaiderVisual : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform leftArm;
        [SerializeField] Transform rightArm;
        [SerializeField] Transform gunModel;

        public void SetSurrendered()
        {
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, 160f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, -160f);
            DropGun();
        }

        public void SetDead()
        {
            DropGun();
            if (body == null) return;
            body.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.localPosition = new Vector3(0f, 0.3f, 0f);
        }

        void DropGun()
        {
            if (gunModel == null || gunModel.parent == null) return;
            Transform root = transform;
            gunModel.SetParent(null, true);
            gunModel.position = root.position + root.forward * 0.7f + Vector3.up * 0.06f;
            gunModel.rotation = Quaternion.Euler(0f, root.eulerAngles.y + 70f, 90f);
        }
    }
}
