using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class StateMachineRunner : MonoBehaviour
    {
        private List<IStateMachine<StateDriverRunner>> stateMachineList =
            new List<IStateMachine<StateDriverRunner>>();

        /// <summary>
        /// Creates a stateMachine token object which is used to managed to the state of a monobehaviour.
        /// </summary>
        /// <typeparam name="TState">An Enum listing different state transitions</typeparam>
        /// <param name="component">The component whose state will be managed</param>
        /// <returns></returns>
        public StateMachine<TState> Initialize<TState>(MonoBehaviour component)
            where TState : struct, IConvertible, IComparable
        {
            var fsm = new StateMachine<TState>(component);

            stateMachineList.Add(fsm);

            return fsm;
        }

        /// <summary>
        /// Creates a stateMachine token object which is used to managed to the state of a monobehaviour. Will automatically transition the startState
        /// </summary>
        /// <typeparam name="TState">An Enum listing different state transitions</typeparam>
        /// <param name="component">The component whose state will be managed</param>
        /// <param name="startState">The default start state</param>
        /// <returns></returns>
        public StateMachine<TState> Initialize<TState>(MonoBehaviour component, TState startState)
            where TState : struct, IConvertible, IComparable
        {
            var fsm = Initialize<TState>(component);

            fsm.ChangeState(startState);

            return fsm;
        }

        void FixedUpdate()
        {
            for (int i = 0; i < stateMachineList.Count; i++)
            {
                var fsm = stateMachineList[i];
                if (!fsm.IsInTransition && fsm.Component.enabled)
                {
                    fsm.Driver.FixedUpdate.Invoke();
                }
            }
        }

        void Update()
        {
            for (int i = 0; i < stateMachineList.Count; i++)
            {
                var fsm = stateMachineList[i];
                if (!fsm.IsInTransition && fsm.Component.enabled)
                {
                    fsm.Driver.Update.Invoke();
                }
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < stateMachineList.Count; i++)
            {
                var fsm = stateMachineList[i];
                if (!fsm.IsInTransition && fsm.Component.enabled)
                {
                    fsm.Driver.LateUpdate.Invoke();
                }
            }
        }

        public static void DoNothing() { }

        public static IEnumerator DoNothingCoroutine()
        {
            yield break;
        }
    }
}
