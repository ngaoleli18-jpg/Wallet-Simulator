using System;

class InsufficientFundsException : Exception
{
    public decimal Needed { get; }
    public decimal Available { get; }

    public InsufficientFundsException(decimal needed, decimal available)
        : base($"Insufficient funds. You need KSh {needed:N2} (including fee) but have KSh {available:N2}.")
    {
        Needed = needed;
        Available = available;
    }
}