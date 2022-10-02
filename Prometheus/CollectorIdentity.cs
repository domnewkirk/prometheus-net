﻿namespace Prometheus
{
    /// <summary>
    /// Represents the values that make up a single collector's unique identity, used during collector registration.
    /// If these values match an existing collector, we will reuse it (or throw if metadata mismatches).
    /// If these values do not match any existing collector, we will create a new collector.
    /// </summary>
    internal struct CollectorIdentity : IEquatable<CollectorIdentity>
    {
        public readonly string Name;
        public readonly string[] LabelNames;
        
        private readonly int _hashCode;

        public CollectorIdentity(string name, string[] labelNames)
        {
            Name = name;
            LabelNames = labelNames;

            _hashCode = CalculateHashCode();
        }

        public bool Equals(CollectorIdentity other)
        {
            if (!string.Equals(Name, other.Name, StringComparison.InvariantCulture))
                return false;

            if (_hashCode != other._hashCode)
                return false;

            if (LabelNames.Length != other.LabelNames.Length)
                return false;

            for (var i = 0; i < LabelNames.Length; i++)
                if (!string.Equals(LabelNames[i], other.LabelNames[i], StringComparison.InvariantCulture))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private int CalculateHashCode()
        {
            unchecked
            {
                int hashCode = 0;

                hashCode ^= Name.GetHashCode() * 31;

                for (int i = 0; i < LabelNames.Length; i++)
                {
                    hashCode ^= (LabelNames[i].GetHashCode() * 397);
                }

                return hashCode;
            }
        }
    }
}
