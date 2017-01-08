using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    class ValueHasChanged<T>
    {
        /// <summary>
        /// The value evaluator
        /// </summary>
        Func<T> _evaluate;
        public ValueHasChanged(Func<T> evaluate)
        {
            _evaluate = evaluate;
        }

        bool _hasBeenEvaluated = false;
        T _value;

        public void Evaluate()
        {
            _value = _evaluate();
            _hasBeenEvaluated = true;
        }

        /// <summary>
        /// Returns true if the value has changed. Will cause a re-calculation of the
        /// value.
        /// </summary>
        public bool HasChanged
        {
            get
            {
                if (!_hasBeenEvaluated)
                {
                    Evaluate();
                    return false;
                } else
                {
                    var old = _value;
                    Evaluate();
                    return !old.Equals(_value);
                }
            }
        }
    }
}
