using System;

enum TransactionType
{
    Deposit,
    Withdraw,
    Send,
    Receive
}

class Transaction
{
    public string Id { get; set; } = "";
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime Date { get; set; }
    public string Counterparty { get; set; } = "";

    public override string ToString()
    {
        return $"{Date:dd/MM/yyyy HH:mm} | {Id} | {Type,-8} | KSh {Amount,10:N2} | Fee {Fee,6:N2} | Bal {BalanceAfter,10:N2} | {Counterparty}";
    }
}