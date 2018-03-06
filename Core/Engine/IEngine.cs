using System;
using System.Collections.Generic;
using System.Text;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
    public interface IEngine
    {
	    void AcceptTransactions(TransactionSigned[] transactions, string sender);
    }
}
