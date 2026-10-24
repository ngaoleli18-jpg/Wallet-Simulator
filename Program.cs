using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static List<Wallet> wallets = new();

    static void Main()
    {
        wallets = WalletStorage.Load();

        while (true)
        {
            Console.WriteLine("\n=== MOBILE WALLET ===");
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("0. Exit");
            Console.Write("Choose: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                    Register();
                    break;
                case "2":
                    Login();
                    break;
                case "0":
                    WalletStorage.Save(wallets);
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    // ---------- Register / Login ----------

    static void Register()
    {
        string phone = ReadPhone("Phone number (e.g. 0712345678): ");

        if (FindWallet(phone) != null)
        {
            Console.WriteLine("A wallet with that number already exists.");
            return;
        }

        string pin = ReadPin("Choose a 4-digit PIN: ");
        string confirm = ReadPin("Confirm PIN: ");

        if (pin != confirm)
        {
            Console.WriteLine("PINs do not match. Registration cancelled.");
            return;
        }

        wallets.Add(new Wallet
        {
            PhoneNumber = phone,
            PinHash = Wallet.HashPin(pin, phone)
        });

        WalletStorage.Save(wallets);
        Console.WriteLine("Wallet created. You can now log in.");
    }

    static void Login()
    {
        string phone = ReadPhone("Phone number: ");
        var wallet = FindWallet(phone);

        if (wallet == null)
        {
            Console.WriteLine("No wallet found for that number.");
            return;
        }

        if (wallet.IsLocked)
        {
            Console.WriteLine("This wallet is locked after too many wrong PIN attempts.");
            return;
        }

        string pin = ReadPin("PIN: ");

        if (!wallet.VerifyPin(pin))
        {
            WalletStorage.Save(wallets);

            if (wallet.IsLocked)
                Console.WriteLine("Wrong PIN. Your wallet is now locked.");
            else
                Console.WriteLine($"Wrong PIN. {Wallet.MaxPinAttempts - wallet.FailedAttempts} attempt(s) left.");

            return;
        }

        WalletStorage.Save(wallets);
        WalletMenu(wallet);
    }

    // ---------- Wallet menu ----------

    static void WalletMenu(Wallet wallet)
    {
        while (true)
        {
            Console.WriteLine($"\n=== {wallet.PhoneNumber} ===");
            Console.WriteLine("1. Send money");
            Console.WriteLine("2. Withdraw");
            Console.WriteLine("3. Deposit");
            Console.WriteLine("4. Check balance");
            Console.WriteLine("5. Mini statement");
            Console.WriteLine("6. Logout");
            Console.Write("Choose: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                    SendMoney(wallet);
                    break;
                case "2":
                    WithdrawMoney(wallet);
                    break;
                case "3":
                    DepositMoney(wallet);
                    break;
                case "4":
                    Console.WriteLine($"Balance: KSh {wallet.Balance:N2}");
                    break;
                case "5":
                    MiniStatement(wallet);
                    break;
                case "6":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    static void SendMoney(Wallet wallet)
    {
        string phone = ReadPhone("Recipient phone number: ");
        var recipient = FindWallet(phone);

        if (recipient == null)
        {
            Console.WriteLine("Recipient not found.");
            return;
        }

        decimal amount = ReadAmount("Amount (KSh): ");
        decimal fee = Wallet.SendFee(amount);

        Console.WriteLine($"Send KSh {amount:N2} to {phone}? Fee: KSh {fee:N2}");
        if (!ConfirmWithPin(wallet))
            return;

        try
        {
            wallet.Send(recipient, amount);
            WalletStorage.Save(wallets);
            Console.WriteLine($"Sent KSh {amount:N2} to {phone}. New balance: KSh {wallet.Balance:N2}");
        }
        catch (InsufficientFundsException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void WithdrawMoney(Wallet wallet)
    {
        decimal amount = ReadAmount("Amount to withdraw (KSh): ");
        decimal fee = Wallet.WithdrawFee(amount);

        Console.WriteLine($"Withdraw KSh {amount:N2}? Fee: KSh {fee:N2}");
        if (!ConfirmWithPin(wallet))
            return;

        try
        {
            wallet.Withdraw(amount);
            WalletStorage.Save(wallets);
            Console.WriteLine($"Withdrawn KSh {amount:N2}. New balance: KSh {wallet.Balance:N2}");
        }
        catch (InsufficientFundsException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void DepositMoney(Wallet wallet)
    {
        decimal amount = ReadAmount("Amount to deposit (KSh): ");

        try
        {
            wallet.Deposit(amount);
            WalletStorage.Save(wallets);
            Console.WriteLine($"Deposited KSh {amount:N2}. New balance: KSh {wallet.Balance:N2}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void MiniStatement(Wallet wallet)
    {
        if (wallet.Transactions.Count == 0)
        {
            Console.WriteLine("No transactions yet.");
            return;
        }

        Console.WriteLine("\nLast 5 transactions:");
        foreach (var t in wallet.Transactions.OrderByDescending(t => t.Date).Take(5))
        {
            Console.WriteLine(t);
        }

        Console.WriteLine($"\nBalance: KSh {wallet.Balance:N2}");
    }

    // ---------- Helpers ----------

    static Wallet? FindWallet(string phone)
    {
        return wallets.FirstOrDefault(w => w.PhoneNumber == phone);
    }

    // Asks for the PIN again before money moves. Counts wrong attempts and locks the wallet.
    static bool ConfirmWithPin(Wallet wallet)
    {
        string pin = ReadPin("Enter PIN to confirm: ");

        if (wallet.VerifyPin(pin))
        {
            WalletStorage.Save(wallets);
            return true;
        }

        WalletStorage.Save(wallets);

        if (wallet.IsLocked)
            Console.WriteLine("Wrong PIN. Your wallet is now locked.");
        else
            Console.WriteLine($"Wrong PIN. {Wallet.MaxPinAttempts - wallet.FailedAttempts} attempt(s) left.");

        return false;
    }

    static string ReadPhone(string message)
    {
        while (true)
        {
            Console.Write(message);
            string phone = Console.ReadLine()?.Trim() ?? "";

            if (Regex.IsMatch(phone, @"^0[17]\d{8}$"))
                return phone;

            Console.WriteLine("Enter a valid 10-digit number starting with 07 or 01.");
        }
    }

    static decimal ReadAmount(string message)
    {
        while (true)
        {
            Console.Write(message);
            if (decimal.TryParse(Console.ReadLine(), out decimal amount) && amount > 0)
                return amount;

            Console.WriteLine("Enter a valid amount greater than 0.");
        }
    }

    // Reads digits and shows * instead of the PIN.
    static string ReadPin(string message)
    {
        Console.Write(message);
        var sb = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                if (sb.Length == 4)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }

                Console.WriteLine("\nPIN must be exactly 4 digits.");
                Console.Write(message);
                sb.Clear();
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
            }
            else if (char.IsDigit(key.KeyChar) && sb.Length < 4)
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}