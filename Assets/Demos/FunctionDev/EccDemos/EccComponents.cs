// using Core.Extensions;
// using R3;
// using UnityEngine;
// using VanishingGames.ECC.Runtime;

// namespace EccDemo
// {
//     public class MoveComponent : EccComponent
//     {
//         protected override void OnSetup()
//         {
//             var rb = mGameObject.GetOrAddComponentRecursively<Rigidbody>();

//             Observable
//                 .EveryUpdate()
//                 .Subscribe(_ =>
//                 {
//                     rb.AddForce(mTransform.forward * speed, ForceMode.Acceleration);
//                     mTransform.position += rb.velocity * Time.deltaTime;
//                 });

//             Observable
//                 .EveryUpdate(UnityFrameProvider.EarlyUpdate)
//                 .Subscribe(_ => Core.Logger.LogInfo("FixedUpdate"));

//             Observable
//                 .EveryValueChanged(rb, rb => rb.velocity)
//                 .Subscribe(_ => rb.velocity = Vector3.zero);
//         }

//         public float speed;
//     }

//     public class HealthComponent : EccComponent
//     {
//         public int hp;
//     }
// }
