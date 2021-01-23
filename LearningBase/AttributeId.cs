using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AiEngine.LearningBase
{
    public class AttributeId : UniqueId
    {
    }

    public class AttributeValueId : UniqueId
    {
    }

    public class ClassificationValueId : UniqueId
    {
    }

    [DebuggerDisplay("{Id}:{Name} - {string.Join(',', Values)}")]
    public class UniqueAttribute<TInstanceIdType, TValueIdType> where TValueIdType : UniqueId, new() where TInstanceIdType : new()
    {
        private readonly Dictionary<TValueIdType, string> _values = new Dictionary<TValueIdType, string>();

        public TInstanceIdType Id { get; }
        public string Name { get; }
        public List<string> Values => _values.Values.ToList();
        public List<TValueIdType> ValueIds => _values.Keys.ToList();

        public Dictionary<TValueIdType, string> ValueMap => _values;

        public TValueIdType GetValueId(
            string value
        ) =>
            _values.Keys.FirstOrDefault(
                key =>
                    string.Compare(
                        value,
                        _values[key],
                        StringComparison.InvariantCultureIgnoreCase) == 0);

        public string GetValue(
            in TValueIdType id
        )

        {
            if (id == null)
            {
                return null;
            }

            if (!_values.ContainsKey(id))
            {
                return null;
            }

            return _values[id]; // $TODO - protect against missing Ids?
        }

        public UniqueAttribute(
            string name,
            IEnumerable<string> values
        )
        {
            Id = new TInstanceIdType();
            Name = name;

            foreach (string valueName in values)
            {
                _values.Add(new TValueIdType(), valueName);
            }
        }
    }

    public class LearningAttribute : UniqueAttribute<AttributeId, AttributeValueId>
    {
        public LearningAttribute(
            string name,
            IEnumerable<string> values
        ) : base(name, values)
        {
        }
    }

    public class Classification : UniqueAttribute<AttributeId, ClassificationValueId>
    {
        public Classification(
            IEnumerable<string> classifications
        ) : base(nameof(Classification), classifications)
        {
        }
    }

    [DebuggerDisplay("{Id}")]
    public class UniqueId : IEquatable<UniqueId>
    {
        private static int _current = 0;

        public int Id { get; }

        public UniqueId()
        {
            Id = _current++;
        }

        public bool Equals(
            UniqueId other
        ) =>
            other != null
            && other.GetType() == this.GetType()
            && other.Id == Id;

        public override bool Equals(
            object obj
        ) =>
            obj != null
            && obj.GetType() == this.GetType()
            && (obj as UniqueId)?.Id == Id;

        public override int GetHashCode() =>
            Id;
    }
}