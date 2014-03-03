﻿// ***********************************************************************
// Copyright (c) 2014 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework.Interfaces;

namespace NUnit.Framework.Internal.Commands
{
    /// <summary>
    /// SetUpTearDownNode holds the setup and teardown methods
    /// for a single level of the hierarchy, together with
    /// a pointer to the next level.
    /// </summary>
    public class SetUpTearDownNode
    {
        private MethodInfo[] _setUpMethods;
        private MethodInfo[] _tearDownMethods;
        private bool _setUpWasRun;

        /// <summary>
        /// Construct a SetUpTearDownNode
        /// </summary>
        /// <param name="fixtureType">The Type of the fixture to which the node applies</param>
        /// <param name="setupType">The Type of the attribute used to mark setup methods</param>
        /// <param name="teardownType">The Type of the attribute used to mark teardown methods</param>
        public SetUpTearDownNode(Type fixtureType, Type setupType, Type teardownType)
        {
            _setUpMethods = Reflect.GetMethodsWithAttribute(fixtureType, setupType, false);
            _tearDownMethods = Reflect.GetMethodsWithAttribute(fixtureType, teardownType, false);
        }

        /// <summary>
        /// Reference to the next level SetUpTearDownNode or null
        /// </summary>
        public SetUpTearDownNode Next { get; set; }

        /// <summary>
        ///  Returns true if this level has any methods at all.
        ///  This flag is used to discard levels that do nothing
        ///  with the exception of the topmost level.
        /// </summary>
        public bool HasMethods
        {
            get { return _setUpMethods.Length > 0 || _tearDownMethods.Length > 0; }
        }

        /// <summary>
        /// Run SetUp on this level after running that for the next lower level.
        /// </summary>
        /// <param name="context">The execution context to use for running.</param>
        public void RunSetUp(TestExecutionContext context)
        {
            // We have not yet run this level
            this._setUpWasRun = false;

            if (Next != null)
                Next.RunSetUp(context);

            // No exception, proceed with this level
            this._setUpWasRun = true;

            foreach (MethodInfo setUpMethod in _setUpMethods)
                Reflect.InvokeMethod(setUpMethod, setUpMethod.IsStatic ? null : context.TestObject);
        }

        /// <summary>
        /// Run TearDown for this level and follow up by running
        /// the next level (base class) teardown.
        /// </summary>
        /// <param name="context"></param>
        public void RunTearDown(TestExecutionContext context)
        {
            // As of NUnit 3.0, we will only run teardown at a given
            // inheritance level if we actually ran setup at that level.
            if (this._setUpWasRun)
                try
                {
                    // Even though we are only running one level at a time, we
                    // run the teardowns in reverse order to provide consistency.
                    int index = _tearDownMethods.Length;
                    while (--index >= 0)
                        Reflect.InvokeMethod(_tearDownMethods[index], _tearDownMethods[index].IsStatic ? null : context.TestObject);
                }
                catch (Exception ex)
                {
                    context.CurrentResult.RecordTearDownException(ex);
                }

            if (Next != null)
                Next.RunTearDown(context);
        }
    }
}
