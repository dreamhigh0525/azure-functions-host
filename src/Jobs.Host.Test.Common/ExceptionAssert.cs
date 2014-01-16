﻿using System;
using Xunit;

namespace Microsoft.WindowsAzure.Jobs.Test
{
    public static class ExceptionAssert
    {
        public static void DoesNotThrow(Action action)
        {
            Assert.DoesNotThrow(() => action.Invoke());
        }

        public static void ThrowsArgument(Action action, string expectedParameterName, string expectedMessage)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() => action.Invoke());
            Assert.Equal(expectedParameterName, exception.ParamName);
            string fullExpectedMessage = GetFullExpectedArgumentMessage(expectedMessage, expectedParameterName);
            Assert.Equal(fullExpectedMessage, exception.Message);
        }

        public static void ThrowsArgumentNull(Action action, string expectedParameterName)
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => action.Invoke());
            Assert.Equal(expectedParameterName, exception.ParamName);
        }

        public static void ThrowsArgumentOutOfRange(Action action, string expectedParameterName, string expectedMessage)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => action.Invoke());
            Assert.Equal(expectedParameterName, exception.ParamName);
            string fullExpectedMessage = GetFullExpectedArgumentMessage(expectedMessage, expectedParameterName);
            Assert.Equal(fullExpectedMessage, exception.Message);
        }

        public static void ThrowsFormat(Action action, string expectedMessage)
        {
            var exception = Assert.Throws<FormatException>(() => action.Invoke());
            Assert.Equal(expectedMessage, exception.Message);
        }

        public static void ThrowsInvalidOperation(Action action, string expectedMessage)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => action.Invoke());
            Assert.Equal(expectedMessage, exception.Message);
        }

        public static void ThrowsObjectDisposed(Action action)
        {
            Assert.Throws<ObjectDisposedException>(() => action.Invoke());
        }

        private static string GetFullExpectedArgumentMessage(string message, string parameterName)
        {
            return String.Format("{0}{1}Parameter name: {2}", message, Environment.NewLine, parameterName);
        }
    }
}
