using System;
using System.Collections.Generic;
using System.Text;
using BlockChanPro.Core;
using BlockChanPro.Core.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlockChanPro.MSTESTS
{
	[TestClass]
	public class BlockValidationTests
	{

		[TestMethod]
		public void GenerateBlock_Validate_Hash()
		{
			var _dependencies = new DependencyContainer("localhost:5000", new NullFeedBack());
			_dependencies.Engine.

		}
		[TestMethod]
		public void GenerateBlock_InvalidTargetDelta_Validate_InvalidHash()
		{

		}

	}
}