using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

class Wallet
{
    public const int MaxPinAttempts = 3;
    public const decimal MaxTransaction = 250000m;

    public string PhoneNumber { get; set; } = "";
    public string PinHash { get; set; } = "";
    public decimal Balance { get; set; }
    public int FailedAttempts { get; set; }
    public bool IsLocked { get; set; }
    public List<Transaction> Transactions { get; set; } = new();

    // ---------- PIN ----------

    public static string HashPin(string pin, string phone)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(phone + ":" + pin);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public bool VerifyPin(string pin)
    {
        if (IsLocked)
            return false;

        if (HashPin(pin, PhoneNumber) == PinHash)
        {
            FailedAttempts = 0;
            return true;
        }

        FailedAttempts++;
        if (FailedAttempts >= MaxPinAttempts)
            IsLocked = true;

        return false;
    }

    // ---------- Simulated fee tables (not real M-Pesa rates) ----------

    public static decimal SendFee(decimal amount) => amount switch
    {
        <= 100m => 0m,
        <= 500m => 7m,
        <= 1000m => 13m,
        <= 5000m => 30m,
        <= 20000m => 50m,
        _ => 100m
    };

    public static decimal WithdrawFee(decimal amount) => amount switch
    {
        <= 500m => 10m,
        <= 2500m => 30m,
        <= 10000m => 60m,
        _ => 100m
    };

    // ---------- Operations ----------

    public void Deposit(decimal amount)
    {
        ValidateAmount(amount);

        Balance += amount;
        Record(TransactionType.Deposit, amount, 0m, "Agent");
    }

    public void Withdraw(decimal amount)
    {
        ValidateAmount(amount);

        decimal fee = WithdrawFee(amount);
        decimal total = amount + fee;

        if (Balance < total)
            throw new InsufficientFundsException(total, Balance);

        Balance -= total;
        Record(TransactionType.Withdraw, amount, fee, "Agent");
    }

    public void Send(Wallet recipient, decimal amount)
    {
        ValidateAmount(amount);

        if (recipient.PhoneNumber == PhoneNumber)
            throw new ArgumentException("You cannot send money to yourself.");

        decimal fee = SendFee(amount);
        decimal total = amount + fee;

        if (Balance < total)
            throw new InsufficientFundsException(total, Balance);

        Balance -= total;
        Record(TransactionType.Send, amount, fee, recipient.PhoneNumber);

        recipient.Balance += amount;
        recipient.Record(TransactionType.Receive, amount, 0m, PhoneNumber);
    }

    // ---------- Helpers ----------

    static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        if (amount > MaxTransaction)
            throw new ArgumentException($"Maximum per transaction is KSh {MaxTransaction:N0}.");
    }

    void Record(TransactionType type, decimal amount, decimal fee, string counterparty)
    {
        Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Type = type,
            Amount = amount,
            Fee = fee,
            BalanceAfter = Balance,
            Date = DateTime.Now,
            Counterparty = counterparty
        });
    }
}