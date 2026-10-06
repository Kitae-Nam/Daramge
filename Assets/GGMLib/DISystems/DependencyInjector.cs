using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGMLib.DISystems
{
    [DefaultExecutionOrder(-10)] //다른 스크립트보다 무조건 먼저 실행되도록, 기본이 0
    public class DependencyInjector : MonoBehaviour
    {
        private const BindingFlags _bindingFlags 
            = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

        private void Awake()
        {
            IEnumerable<IDependencyProvider> providers = FindMonoBehaviours().OfType<IDependencyProvider>();

            foreach(IDependencyProvider provider in providers)
            {
                RegisterProvider(provider);
            }

            IEnumerable<MonoBehaviour> injectables = FindMonoBehaviours().Where(IsInjectable);
            foreach(MonoBehaviour target in injectables)
            {
                Inject(target);
            }
        }
        
        private void Inject(MonoBehaviour target)
        {
            Type type = target.GetType();
            IEnumerable<FieldInfo> fields = type.GetFields(_bindingFlags)
                .Where(field => Attribute.IsDefined(field, typeof(InjectAttribute)));

            foreach(FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                object injectInstance = Resolve(fieldType);
                Debug.Assert(injectInstance != null, $"주입할 오브젝트가 없습니다. : {fieldType}");
                
                field.SetValue(target, injectInstance);
            }
            
            //주입이 필요한 매서드들에 대해서 수행하기
            IEnumerable<MethodInfo> methods = type.GetMethods(_bindingFlags)
                .Where(method => Attribute.IsDefined(method, typeof(InjectAttribute)));

            foreach (MethodInfo method in methods)
            {
                Type[] requiredParams = method.GetParameters().Select(p => p.ParameterType).ToArray();
                object[] paramInstances = requiredParams.Select(Resolve).ToArray();
                method.Invoke(target, paramInstances);
            }
        }
        
        private object Resolve(Type fieldType)
        {
            _registry.TryGetValue(fieldType, out object instance);
            return instance;
        }

        private bool IsInjectable(MonoBehaviour mono)
        {
            MemberInfo[] members = mono.GetType().GetMembers(_bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }

        private void RegisterProvider(IDependencyProvider provider)
        {
            //클래스 자체가 Provide어트리뷰트가 붙었다.
            if (Attribute.IsDefined(provider.GetType(), typeof(ProvideAttribute)))
            {
                _registry.Add(provider.GetType(), provider);
                return;
            }
            
            //그렇지 않다면 해당 클래스에 Provide가 붙은 매서드가 있는지를 찾아야 해.
            MethodInfo[] methods = provider.GetType().GetMethods(_bindingFlags);

            foreach(MethodInfo method in methods)
            {
                if(!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                
                Type returnType = method.ReturnType;
                object providedInstance = method.Invoke(provider, null);
                Debug.Assert(providedInstance != null, $"제공된 인스턴스가 널입니다. : {provider},  {method.Name}");
                
                _registry.Add(returnType, providedInstance);
            }
        }

        private MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        }
    }
}