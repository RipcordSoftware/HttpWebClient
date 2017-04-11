//The MIT License(MIT)
//
//Copyright(c) 2015-2017 Ripcord Software Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;

namespace RipcordSoftware.HttpWebClient
{
    /// <summary>
    /// A very simple (Pure) DI container
    /// </summary>
    /// <remarks>So we don't closely couple to any other container</remarks>
    public static class HttpWebClientContainer
    {
        #region Private fields
        private static readonly Dictionary<Type, Type> _typeMapper = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Delegate> _delegateMapper = new Dictionary<Type, Delegate>();
        #endregion

        #region Public methods
        /// <summary>
        /// Clear any registered types
        /// </summary>
        /// <remarks>Only useful for test tools</remarks>
        public static void Clear()
        {
            _typeMapper.Clear();
            _delegateMapper.Clear();
        }

        /// <summary>
        /// Registers an interface against a concrete type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="T"></typeparam>
        public static void Register<I, T>() where T : I
        {
            _typeMapper[typeof(I)] = typeof(T);
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<object, I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<object, object, I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<object, object, object, I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<object, object, object, object, I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Registers a delegate against an interface type
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="callback"></param>
        public static void Register<I>(Func<object, object, object, object, object, I> callback)
        {
            _delegateMapper[typeof(I)] = callback;
        }

        /// <summary>
        /// Resolves a concrete implementation of an interface
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <returns></returns>
        public static I Resolve<I>()
        {
            Type type = null;
            if (_typeMapper.TryGetValue(typeof(I), out type))
            {
                return (I)Activator.CreateInstance(type);
            }
            else
            {
                Delegate @delegate = null;
                if (_delegateMapper.TryGetValue(typeof(I), out @delegate))
                {
                    return (I)@delegate.DynamicInvoke(null);
                }
            }

            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Resolves a concrete implementation of an interface, passing parameters to the constructor
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        public static I Resolve<I>(params object[] args)
        {
            Type type = null;
            if (_typeMapper.TryGetValue(typeof(I), out type))
            {
                return (I)Activator.CreateInstance(type, args);
            }
            else
            {
                Delegate @delegate = null;
                if (_delegateMapper.TryGetValue(typeof(I), out @delegate))
                {
                    return (I)@delegate.DynamicInvoke(args);
                }
            }

            throw new ArgumentOutOfRangeException();
        }
        #endregion
    }
}
