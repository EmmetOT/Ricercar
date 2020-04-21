using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnibusEvent;

namespace UnibusEvent
{
    public delegate void OnEvent<T, U>(T action1, U action2);

    public delegate void OnEvent<T>(T action);

    public delegate void OnEvent();

    public delegate void OnEventTwoParamWrapper(object _object1, object _object2);

    public delegate void OnEventWrapper(object _object);

    public delegate void OnNoParamEventWrapper();

    public class DictionaryKey
    {
        public Type Type1;
        public Type Type2;
        public object Tag;

        public DictionaryKey(string tag, Type type1, Type type2 = null)
        {
            Tag = tag;
            Type1 = type1;
            Type2 = type2;
        }

        public override int GetHashCode()
        {
            return Tag.GetHashCode() ^ (Type1 == null ? 1 : Type1.GetHashCode()) ^ (Type2 == null ? 1 : Type2.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is DictionaryKey)
            {
                DictionaryKey key = (DictionaryKey)obj;
                return Tag.Equals(key.Tag) && ((Type1 == null && key.Type1 == null) || Type1 == key.Type1) && ((Type2 == null && key.Type2 == null) || Type2 == key.Type2);
            }

            return false;
        }

        public override string ToString()
        {
            return Tag + (Type1 != null ? ", " + Type1 + ((Type2 != null ? ", " + Type2 : "")) : "");
        }
    }
    
    public class UnibusObject : Singleton<UnibusObject>
    {
        private Dictionary<DictionaryKey, Dictionary<int, OnEventTwoParamWrapper>> globalTwoParamEventDictionary = new Dictionary<DictionaryKey, Dictionary<int, OnEventTwoParamWrapper>>(new UnibusKeyComparer());

        private Dictionary<DictionaryKey, Dictionary<int, OnEventWrapper>> globalEventDictionary = new Dictionary<DictionaryKey, Dictionary<int, OnEventWrapper>>(new UnibusKeyComparer());

        private Dictionary<DictionaryKey, Dictionary<int, OnNoParamEventWrapper>> globalNoParamEventDictionary = new Dictionary<DictionaryKey, Dictionary<int, OnNoParamEventWrapper>>(new UnibusKeyComparer());

        public void ToggleSubscribed<T, U>(bool toggle, string tag, OnEvent<T, U> eventCallback)
        {
            if (toggle)
                Subscribe(tag, eventCallback);
            else
                Unsubscribe(tag, eventCallback);
        }

        public void ToggleSubscribed<T>(bool toggle, string tag, OnEvent<T> eventCallback)
        {
            if (toggle)
                Subscribe(tag, eventCallback);
            else
                Unsubscribe(tag, eventCallback);
        }

        public void ToggleSubscribed(bool toggle, string tag, OnEvent eventCallback)
        {
            if (toggle)
                Subscribe(tag, eventCallback);
            else
                Unsubscribe(tag, eventCallback);
        }

        public void Subscribe<T, U>(string tag, OnEvent<T, U> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T), typeof(U));

            if (!globalTwoParamEventDictionary.ContainsKey(key))
                globalTwoParamEventDictionary[key] = new Dictionary<int, OnEventTwoParamWrapper>();

            if (globalTwoParamEventDictionary[key].ContainsKey(eventCallback.GetHashCode()))
                return;

            globalTwoParamEventDictionary[key][eventCallback.GetHashCode()] = (object _object1, object _object2) => { eventCallback((T)_object1, (U)_object2); };
        }

        public void Subscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));
            
            if (!globalEventDictionary.ContainsKey(key))
                globalEventDictionary[key] = new Dictionary<int, OnEventWrapper>();

            if (globalEventDictionary[key].ContainsKey(eventCallback.GetHashCode()))
                return;

            globalEventDictionary[key][eventCallback.GetHashCode()] = (object _object) => { eventCallback((T) _object); };
        }

        public void Subscribe(string tag, OnEvent eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, null);

            if (!globalNoParamEventDictionary.ContainsKey(key))
                globalNoParamEventDictionary[key] = new Dictionary<int, OnNoParamEventWrapper>();

            if (globalNoParamEventDictionary[key].ContainsKey(eventCallback.GetHashCode()))
                return;

            globalNoParamEventDictionary[key][eventCallback.GetHashCode()] = () => { eventCallback(); };
        }

        public void Unsubscribe<T, U>(string tag, OnEvent<T, U> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T), typeof(U));

            if (globalTwoParamEventDictionary.ContainsKey(key) && globalTwoParamEventDictionary[key] != null)
            {
                globalTwoParamEventDictionary[key].Remove(eventCallback.GetHashCode());
            }
        }

        public void Unsubscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));

            if (globalEventDictionary.ContainsKey(key) && globalEventDictionary[key] != null)
            {
                globalEventDictionary[key].Remove(eventCallback.GetHashCode());
            }
        }

        public void Unsubscribe(string tag, OnEvent eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, null);

            if (globalNoParamEventDictionary.ContainsKey(key) && globalNoParamEventDictionary[key] != null)
            {
                globalNoParamEventDictionary[key].Remove(eventCallback.GetHashCode());
            }
        }
        
        public void Dispatch(string tag)
        {
            DictionaryKey key = new DictionaryKey(tag, null);
            
            if (globalNoParamEventDictionary.ContainsKey(key))
            {                
                // Q: Why use ToList()? Why not just iterate through the ValueCollection?
                // A: We need to create a copy of the collection because some unibus events
                // will indirectly lead to modification of the dictionary. This will lead to an out of
                // sync error.
                foreach (OnNoParamEventWrapper caller in globalNoParamEventDictionary[key].Values.ToList())
                {
                    caller();
                }
            }
        }

        public void Dispatch<T>(string tag, T action)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));
            
            if (globalEventDictionary.ContainsKey(key))
            {
                // Q: Why use ToList()? Why not just iterate through the ValueCollection?
                // A: We need to create a copy of the collection because some unibus events
                // will indirectly lead to modification of the dictionary. This will lead to an out of
                // sync error.
                foreach (OnEventWrapper caller in globalEventDictionary[key].Values.ToList())
                {
                    caller(action);
                }
            }
        }

        public void Dispatch<T, U>(string tag, T action1, U action2)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T), typeof(U));

            if (globalTwoParamEventDictionary.ContainsKey(key))
            {
                // Q: Why use ToList()? Why not just iterate through the ValueCollection?
                // A: We need to create a copy of the collection because some unibus events
                // will indirectly lead to modification of the dictionary. This will lead to an out of
                // sync error.
                foreach (OnEventTwoParamWrapper caller in globalTwoParamEventDictionary[key].Values.ToList())
                {
                    caller(action1, action2);
                }
            }
        }

        public void ClearEvents()
		{
			globalEventDictionary.Clear();
			globalNoParamEventDictionary.Clear();
			globalTwoParamEventDictionary.Clear();
        }
    }
}

public struct UnibusKeyComparer : IEqualityComparer<DictionaryKey>
{
    bool IEqualityComparer<DictionaryKey>.Equals(DictionaryKey x, DictionaryKey y)
    {
        return x.GetHashCode() == y.GetHashCode();
    }

    int IEqualityComparer<DictionaryKey>.GetHashCode(DictionaryKey obj)
    {
        return obj.GetHashCode();
    }
}