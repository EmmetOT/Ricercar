using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnibusEvent
{
    public static class Unibus
    {
        public static void Subscribe<T, U>(string tag, OnEvent<T, U> eventCallback)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback);
        }

        public static void Subscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback);
        }

        public static void Subscribe(string tag, OnEvent eventCallback)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback);
        }
        
        public static void Unsubscribe<T, U>(string tag, OnEvent<T, U> eventCallback)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback);
        }

        public static void Unsubscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback);
        }

        public static void Unsubscribe(string tag, OnEvent eventCallback)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback);
        }
        
        public static void Dispatch<T, U>(string tag, T action1, U action2)
        {
            UnibusObject.Instance?.Dispatch(tag, action1, action2);
        }

        public static void Dispatch<T>(string tag, T action)
        {
            UnibusObject.Instance?.Dispatch(tag, action);
        }

        public static void Dispatch(string tag)
        {
            UnibusObject.Instance?.Dispatch(tag);
        }
    }
}