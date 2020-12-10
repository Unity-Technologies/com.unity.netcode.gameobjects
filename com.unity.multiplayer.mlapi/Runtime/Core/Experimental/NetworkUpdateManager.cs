using System;
using System.Text;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;

namespace MLAPI
{
    /// <summary>
    /// Allows one to create their own network update engine
    /// in the event they have specific processing needs etc.
    /// </summary>
    public interface INetworkUpdateEngine
    {
        void PreUpdate();
        void PreUpdateRegister(Action updateAction);

        void FixedUpdate();
        void FixedUpdateRegister(Action updateAction);

        void Update();
        void UpdateRegister(Action updateAction);

        void PostUpdate();
        void PostUpdateRegister(Action updateAction);

    }

    /// <summary>
    /// InternalNetworkUpdateEngine
    /// Basic-Default version of an INetworkUpdateEngine implementation
    /// This allows for anyone to come up with custom variations, which could include allowing multiple actions to be registered
    /// </summary>
    internal class InternalNetworkUpdateEngine:INetworkUpdateEngine
    {
        public Action PreUpdateAction;
        public Action PostPreUpdateAction;
        public Action FixedUpdateAction;
        public Action UpdateAction;
        public Action PostUpdateAction;

        /// <summary>
        /// Handle receiving of any pending packets
        /// </summary>
        public void PreUpdate()
        {
            if(PreUpdateAction != null)
            {
                PreUpdateAction.Invoke();
            }
        }

        public void PreUpdateRegister(Action updateAction)
        {
            PreUpdateAction = updateAction;
        }

        public void FixedUpdate()
        {
            if(FixedUpdateAction != null)
            {
                FixedUpdateAction.Invoke();
            }
        }
        public void FixedUpdateRegister(Action updateAction)
        {
            FixedUpdateAction = updateAction;
        }

        public void Update()
        {
            if(UpdateAction != null)
            {
                UpdateAction.Invoke();
            }
        }
        public void UpdateRegister(Action updateAction)
        {
            UpdateAction = updateAction;
        }
                
        public void PostUpdate()
        {

            if(PostUpdateAction != null)
            {
                PostUpdateAction.Invoke();
            }
        }

        public void PostUpdateRegister(Action updateAction)
        {
            PostUpdateAction = updateAction;
        }
    }


    /// <summary>
    /// NetworkUpdateManager
    /// External public facing class for the registration of and processing of network updates
    /// Extended to allow for customized INetworkUpdateEngine implementations
    /// </summary>
    public class NetworkUpdateManager
    {
        public enum NetworkUpdateStages
        {
            PREUPDATE,      //Invoked after EarlyUpdate.UnityWebRequestUpdate
            FIXEDUPDATE,    //Invoked after FixedUpdate.AudioFixedUpdate (prior to any physics being applied or simulated)
            UPDATE,         //Invoked after PreUpdate.UpdateVideo   (just before the primary Update is invoked)
            LATEUPDATE      //Invoked after PostLateUpdate.ProcessWebSendMessages (after all updates)
        }

        static INetworkUpdateEngine CurrentNetworkUpdateEngine;

        /// <summary>
        /// AssignNetworkUpdateEngine
        /// Provides the option of passing in a custom network update engine
        /// </summary>
        /// <param name="updateEngine">INetworkUpdateEngine interface derived class</param>
        public static void AssignNetworkUpdateEngine(INetworkUpdateEngine updateEngine)
        {
            CurrentNetworkUpdateEngine = updateEngine;
        }

        /// <summary>
        /// RegisterNetworkUpdateAction
        /// Registers an action to a specific update stage
        /// </summary>
        /// <param name="updateAction">action to apply</param>
        /// <param name="updateStage">update stage to apply the action to</param>
        public static void RegisterNetworkUpdateAction(Action updateAction, NetworkUpdateStages updateStage)
        {
            if(CurrentNetworkUpdateEngine == null)
            {
                CurrentNetworkUpdateEngine = new InternalNetworkUpdateEngine();
            }

            switch(updateStage)
            {
                case NetworkUpdateStages.PREUPDATE:
                    {
                         CurrentNetworkUpdateEngine.PreUpdateRegister(updateAction);
                        break;
                    }
                case NetworkUpdateStages.FIXEDUPDATE:
                    {
                         CurrentNetworkUpdateEngine.FixedUpdateRegister(updateAction);
                        break;
                    }
                case NetworkUpdateStages.UPDATE:
                    {
                        CurrentNetworkUpdateEngine.UpdateRegister(updateAction);
                        break;
                    }
                case NetworkUpdateStages.LATEUPDATE:
                    {
                         CurrentNetworkUpdateEngine.PostUpdateRegister(updateAction);
                        break;
                    }
            }           
        }

        /// <summary>
        /// AppStart
        /// Initial definition of the four primary network update stages
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void AppStart()
        {
            var def = PlayerLoop.GetCurrentPlayerLoop();

            if(CurrentNetworkUpdateEngine == null)
            {
                CurrentNetworkUpdateEngine = new InternalNetworkUpdateEngine();
            }

            Type currentNUEType = CurrentNetworkUpdateEngine.GetType();

            //NetworkUpdateManager Primary PlayerLoop Update Registrations 
            var networkPreUpdateLoop = new PlayerLoopSystem()
            {
                updateDelegate = CurrentNetworkUpdateEngine.PreUpdate,
                type = currentNUEType
            };
            InsertSystem(ref def, networkPreUpdateLoop, typeof(EarlyUpdate.UnityWebRequestUpdate));

            var networkFixedUpdateLoop = new PlayerLoopSystem()
            {
                updateDelegate = CurrentNetworkUpdateEngine.FixedUpdate,
                type = currentNUEType
            };
            InsertSystem(ref def, networkFixedUpdateLoop, typeof(FixedUpdate.AudioFixedUpdate));

            var networkUpdateLoop = new PlayerLoopSystem()
            {
                updateDelegate = CurrentNetworkUpdateEngine.Update,
                type = currentNUEType
            };
            InsertSystem(ref def, networkUpdateLoop, typeof(PreUpdate.UpdateVideo));

            var networkPostUpdateLoop = new PlayerLoopSystem()
            {
                updateDelegate = CurrentNetworkUpdateEngine.PostUpdate,
                type = currentNUEType
            };
            InsertSystem(ref def, networkPostUpdateLoop, typeof(PostLateUpdate.ProcessWebSendMessages));

    #if UNITY_EDITOR
            PrintPlayerLoop(def);
    #endif

            PlayerLoop.SetPlayerLoop(def);
        }
        
        /// <summary>
        /// InsertSystem
        /// Recursively search for the given system type and insert the new system immediately afterwards        
        /// </summary>
        /// <param name="system">PlayerLoopSystem to search</param>
        /// <param name="toInsert">PlayerLoopSystem to insert</param>
        /// <param name="insertAfter">location to insert the PlayerLoopSystem</param>
        /// <returns></returns>
        private static bool InsertSystem(ref PlayerLoopSystem system, PlayerLoopSystem toInsert, Type insertAfter)
        {
            if (system.subSystemList == null)
            {
                return false;
            }

            for (int i = 0; i < system.subSystemList.Length; ++i)
            {
                if (system.subSystemList[i].type == insertAfter)
                {
                    InsertSystemAt(ref system, toInsert, i + 1);
                    return true;
                }
            }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (InsertSystem(ref system.subSystemList[i], toInsert, insertAfter))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// InsertSystemAt
        /// Copies the subSystemList of the given system and inserts a new system at the given index
        /// </summary>
        /// <param name="system">PlayerLoopSystem to be inserted into</param>
        /// <param name="toInsert">PlayerLoopSystem to insert</param>
        /// <param name="pos">position to insert the PlayerLoopSystem</param>
        private static void InsertSystemAt(ref PlayerLoopSystem system, PlayerLoopSystem toInsert, int pos)
        {
            PlayerLoopSystem[] newSubSystems = new PlayerLoopSystem[system.subSystemList.Length + 1];
            for (int i = 0, oldSystemIdx = 0; i < newSubSystems.Length; ++i)
            {
                if (i == pos)
                {
                    newSubSystems[i] = toInsert;
                }
                else
                {
                    newSubSystems[i] = system.subSystemList[oldSystemIdx++];
                }
            }

            system.subSystemList = newSubSystems;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// PrintPlayerLoop
        /// Prints all PlayerLoopSystems within the PlayerLoopSystem provided
        /// </summary>
        /// <param name="pl">PlayerLoopSystem</param>
        private static void PrintPlayerLoop(PlayerLoopSystem pl)
        {
            var sb = new StringBuilder();
            RecursivePlayerLoopPrint(pl, sb, 0);
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// RecursivePlayerLoopPrint
        /// Recursively build the entire PlayerLoopSystem list
        /// </summary>
        /// <param name="def">PlayerLoopSystem to be added</param>
        /// <param name="sb">StringBuilder to add to</param>
        /// <param name="depth">Maximum recursion depth</param>
        private static void RecursivePlayerLoopPrint(PlayerLoopSystem def, StringBuilder sb, int depth)
        {
            if (depth == 0)
            {
                sb.AppendLine("ROOT NODE");
            }
            else if (def.type != null)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("\t");
                }
                sb.AppendLine(def.type.Name);
            }
            if (def.subSystemList != null)
            {
                depth++;
                foreach (var s in def.subSystemList)
                {
                    RecursivePlayerLoopPrint(s, sb, depth);
                }
                depth--;
            }
        }
        #endif
        
    }
}
