using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGMLib.ModuleSystems
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        protected Dictionary<Type, IModule> _moduleDict;

        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IModule>().ToDictionary(module => module.GetType());
            
            InitializeModules();
            AfterInitializeModules();
        }

        protected virtual void InitializeModules()
        {
            foreach(IModule module in _moduleDict.Values)
                module.Initialize(this);
        }

        protected virtual void AfterInitializeModules()
        {
            foreach(IAfterInitModule module in _moduleDict.Values.OfType<IAfterInitModule>())
                module.AfterInit();
        }

        public T GetModule<T>()
        {
            if(_moduleDict.TryGetValue(typeof(T), out IModule module))
                return (T)module;
            
            IModule findedModule = _moduleDict.Values.FirstOrDefault(m => m is T);
            
            if(findedModule is T castedModule)
                return castedModule;
            
            return default(T);
        }
    }
}