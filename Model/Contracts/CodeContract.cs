using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BlockChanPro.Model.Contracts
{
	internal static class Contract
	{
		//
		// Summary:
		//     Specifies a precondition contract for the enclosing method or property, and throws
		//     an exception if the condition for the contract fails.
		//
		// Parameters:
		//   condition:
		//     The conditional expression to test.
		//
		// Type parameters:
		//   TException:
		//     The exception to throw if the condition is false.
		[ContractAnnotation("halt <= condition: false")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Requires<TException>(bool condition)
			where TException : Exception, new()
		{
			if (!condition)
				throw new TException();
		}

		//
		// Summary:
		//     Specifies a precondition contract for the enclosing method or property, and throws
		//     an exception with the provided message if the condition for the contract fails.
		//
		// Parameters:
		//   condition:
		//     The conditional expression to test.
		//
		//   userMessage:
		//     The message to display if the condition is false.
		//
		// Type parameters:
		//   TException:
		//     The exception to throw if the condition is false.
		[ContractAnnotation("halt <= condition: false")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Requires<TException>(bool condition, string userMessage)
			where TException : Exception, new()
		{
			if (condition)
				return;

			var ex = (TException)Activator.CreateInstance(typeof(TException));
			var internalFieldInfo = typeof(TException).GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance);
			if (internalFieldInfo != null)
				internalFieldInfo.SetValue(ex, userMessage);

			throw ex;
		}

		/// <summary>
		/// Validate and throws <see cref="ArgumentException"/> if condition is false
		/// </summary>
		/// <param name="condition">Condition that must be satisfied</param>
		/// <param name="paramName">Name of the parameter that is used in validation</param>
		[ContractAnnotation("halt <= condition: false")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Requires(bool condition, string paramName)
		{
			if (condition)
				return;

			throw new ArgumentException(paramName);
		}

		/// <summary>
		/// Validate and throws <see cref="ArgumentNullException"/> if value is null
		/// </summary>
		/// <param name="value">Value that must not be null</param>
		/// <param name="paramName">Name of the parameter that is used in validation</param>
		[ContractAnnotation("halt <= value: null")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Requires(object value, string paramName)
		{
			if (value != null)
				return;

			throw new ArgumentNullException(paramName);
		}

		public static void EndContractBlock()
		{
		}
	}

	// ReSharper disable once RedundantAttributeUsageProperty
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public class ContractClass : Attribute
	{
		// ReSharper disable once UnusedParameter.Local
		public ContractClass(Type typeContainingContracts)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class ContractClassForAttribute : Attribute
	{
		// ReSharper disable once UnusedParameter.Local
		public ContractClassForAttribute(Type typeContractsAreFor)
		{
		}
	}
}
