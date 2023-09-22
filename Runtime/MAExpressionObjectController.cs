using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [Serializable]
    public abstract class MAExpressionObjectController : MAExpressionBaseComponent
    {
        public abstract string DisplayName { get; }
        public abstract IEnumerable<TargetObject> GetControlObjects();
        public abstract string GetParameterPrefix();
    }
}
