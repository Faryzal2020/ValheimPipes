using System.Collections.Generic;
using UnityEngine;

namespace ValheimPipes.Logic {
    public interface ITarget: IEqualityComparer<ITarget> {
        int NetworkHashCode();
        bool IsValid();
        bool InRange(Vector3 position);
    }
}
