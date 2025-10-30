using Core.Extensions;
using R3;
using UnityEngine;
using VanishingGames.ECC.Runtime;

[System.Serializable]
public class MoveComponent : EccComponent
{
    protected override void OnSetup()
    {
        var rb = mGameObject.GetOrAddComponentRecursively<Rigidbody>();

        Observable
            .EveryUpdate()
            .Subscribe(_ =>
            {
                rb.AddForce(mTransform.forward * speed, ForceMode.Acceleration);
                mTransform.position += rb.velocity * Time.deltaTime;
            });

        Observable
            .EveryUpdate()
            .Subscribe(_ =>
            {
                Core.Logger.LogInfo("FixedUpdate");
            });

        Observable
            .EveryValueChanged(rb, rb => rb.velocity)
            .Subscribe(v => rb.velocity = Vector3.zero);
    }

    public float speed;
}

[System.Serializable]
public class HealthComponent : EccComponent
{
    public int hp;
}
