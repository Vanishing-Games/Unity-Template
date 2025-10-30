using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public interface ISavable { }

    public interface ISaveData { }

    /// <summary>
    /// Save data without reference
    /// </summary>
    public interface ISavableStruct<T> : ISavable
        where T : struct { }

    /// <summary>
    /// Save data with reference
    /// </summary>
    public interface ISavableClass<T> : ISavable
        where T : ISaveData { }
}
