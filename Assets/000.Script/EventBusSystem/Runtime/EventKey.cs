using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Roni.CustomEventSystem.EventBus.Core
{
    // UIEventKey 구조체 
    public readonly struct EventKey<T> : IEquatable<EventKey<T>>
    {
        public string Value { get; }
        public string Description { get; }
        public EventKey(string value, string description) { Value = value; Description = description; }

        // 비교, ToString 등
        public override string ToString() => Value;
        public bool Equals(EventKey<T> other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EventKey<T> other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public static bool operator ==(EventKey<T> left, EventKey<T> right) => left.Equals(right);
        public static bool operator !=(EventKey<T> left, EventKey<T> right) => !left.Equals(right);

    }

}