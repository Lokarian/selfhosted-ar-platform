using MessagePack;
using MessagePack.Resolvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessagePackStartup : MonoBehaviour
{
    static bool serializerRegistered = false;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (!serializerRegistered)
        {
            StaticCompositeResolver.Instance.Register(
                GeneratedResolver.Instance,
                BuiltinResolver.Instance,
                AttributeFormatterResolver.Instance,
                PrimitiveObjectResolver.Instance,
                StandardResolver.Instance
            );
            var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
            MessagePackSerializer.DefaultOptions = option;
            serializerRegistered = true;
        }
    }
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void EditorInitialize()
    {
        Initialize();
    }
#endif
}