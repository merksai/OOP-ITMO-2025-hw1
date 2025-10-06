using System;
using System.Collections.Generic;
using System.Linq;

class Product
{
    public int Id { get; }
    public string Name { get; private set; }
    public int Price { get; private set; }
    public int Quantity { get; private set; }

    public Product(int id, string name, int price, int quantity)
    {
        Id = id;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public void Restock(int amount) => Quantity += amount;
    public void Reprice(int newPrice) => Price = newPrice;
    public bool TakeOne()
    {
        if (Quantity <= 0) return false;
        Quantity--;
        return true;
    }

    public override string ToString()
    {
        return $"{Id}. {Name} — {Price}р ({Quantity} шт.)";
    }
}

class CashBank
{
    readonly int[] denomsDesc;
    readonly Dictionary<int, int> coins;

    public CashBank(IEnumerable<int> denominations, Dictionary<int, int> initial = null)
    {
        denomsDesc = denominations.OrderByDescending(x => x).ToArray();
        coins = denomsDesc.ToDictionary(d => d, d => 0);
        if (initial != null)
            foreach (var kv in initial) coins[kv.Key] = kv.Value;
    }

    public int Total => coins.Sum(kv => kv.Key * kv.Value);

    public void Deposit(Dictionary<int, int> toAdd)
    {
        foreach (var kv in toAdd)
            if (coins.ContainsKey(kv.Key))
                coins[kv.Key] += kv.Value;
    }

    public bool Withdraw(Dictionary<int, int> toTake)
    {
        foreach (var kv in toTake)
            if (!coins.ContainsKey(kv.Key) || coins[kv.Key] < kv.Value) return false;
        foreach (var kv in toTake)
            coins[kv.Key] -= kv.Value;
        return true;
    }

    public Dictionary<int, int> MakeChange(int amount)
    {
        var result = denomsDesc.ToDictionary(d => d, d => 0);
        int remaining = amount;
        foreach (var d in denomsDesc)
        {
            int use = Math.Min(remaining / d, coins[d]);
            if (use > 0)
            {
                result[d] = use;
                remaining -= use * d;
            }
        }
        if (remaining == 0) return result.Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value);
        return null;
    }

    public Dictionary<int, int> Snapshot() => coins.ToDictionary(k => k.Key, v => v.Value);

    public void Clear()
    {
        foreach (var d in denomsDesc) coins[d] = 0;
    }

    public override string ToString()
    {
        var parts = coins.OrderByDescending(k => k.Key).Select(kv => $"{kv.Key}р*{kv.Value}");
        return string.Join(", ", parts) + $" = {Total}р";
    }
}

class VendingMachine
{
    readonly List<Product> products = new List<Product>();
    readonly int[] denominations = new[] { 1, 2, 5, 10, 50, 100 };
    readonly CashBank bank;
    readonly Dictionary<int, int> inserted = new Dictionary<int, int>();
    int nextProductId = 1;

    public VendingMachine()
    {
        bank = new CashBank(denominations, new Dictionary<int, int> { {1,20},{2,20},{5,20},{10,20},{50,10},{100,5} });
        foreach (var d in denominations) inserted[d] = 0;
        AddProduct("Вода", 45, 10);
        AddProduct("Сок", 60, 8);
        AddProduct("Шоколад", 75, 5);
        AddProduct("Чипсы", 50, 6);
    }

    public void AddProduct(string name, int price, int quantity)
    {
        products.Add(new Product(nextProductId++, name, price, quantity));
    }

    public IEnumerable<Product> Products => products;

    public int Balance => inserted.Sum(kv => kv.Key * kv.Value);

    public int[] Denominations => denominations;

    public void InsertCoin(int denom, int count)
    {
        if (!inserted.ContainsKey(denom)) return;
        if (count <= 0) return;
        inserted[denom] += count;
    }

    public Dictionary<int, int> Cancel()
    {
        var back = inserted.Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value);
        foreach (var d in denominations) inserted[d] = 0;
        return back;
    }

    public bool Purchase(int productId, out string message, out Dictionary<int, int> change)
    {
        message = "";
        change = null;
        var p = products.FirstOrDefault(x => x.Id == productId);
        if (p == null) { message = "Товар не найден."; return false; }
        if (p.Quantity <= 0) { message = "Товар закончился."; return false; }
        if (Balance < p.Price) { message = $"Недостаточно средств. Нужна сумма {p.Price}р."; return false; }
        int changeAmount = Balance - p.Price;
        var changePlan = changeAmount == 0 ? new Dictionary<int, int>() : bank.MakeChange(changeAmount);
        if (changePlan == null) { message = "Невозможно выдать сдачу."; return false; }
        bank.Deposit(inserted.Where(kv => kv.Value > 0).ToDictionary(k => k.Key, v => v.Value));
        foreach (var d in denominations) inserted[d] = 0;
        if (changeAmount > 0) bank.Withdraw(changePlan);
        p.TakeOne();
        change = changePlan;
        message = $"Вы получили: {p.Name}.";
        return true;
    }

    public string BankInfo() => bank.ToString();

    public int CollectCash()
    {
        int total = bank.Total;
        bank.Clear();
        return total;
    }

    public bool Restock(int productId, int amount)
    {
        var p = products.FirstOrDefault(x => x.Id == productId);
        if (p == null || amount <= 0) return false;
        p.Restock(amount);
        return true;
    }

    public bool Reprice(int productId, int newPrice)
    {
        var p = products.FirstOrDefault(x => x.Id == productId);
        if (p == null || newPrice <= 0) return false;
        p.Reprice(newPrice);
        return true;
    }
}

class Program
{
    static void Main()
    {
        var vm = new VendingMachine();
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Список товаров");
            Console.WriteLine("2. Внести монеты");
            Console.WriteLine("3. Купить товар");
            Console.WriteLine("4. Отмена и возврат");
            Console.WriteLine("5. Баланс");
            Console.WriteLine("6. Админ режим");
            Console.WriteLine("0. Выход");
            Console.Write("Выбор: ");
            var choice = Console.ReadLine();
            Console.WriteLine();
            if (choice == "0") break;
            if (choice == "1") ShowProducts(vm);
            else if (choice == "2") InsertCoins(vm);
            else if (choice == "3") Buy(vm);
            else if (choice == "4") Cancel(vm);
            else if (choice == "5") Console.WriteLine($"Баланс: {vm.Balance}р");
            else if (choice == "6") Admin(vm);
        }
    }

    static void ShowProducts(VendingMachine vm)
    {
        foreach (var p in vm.Products) Console.WriteLine(p);
    }

    static void InsertCoins(VendingMachine vm)
    {
        Console.WriteLine("Доступные номиналы: " + string.Join(", ", vm.Denominations.Select(d => d + "р")));
        Console.Write("Номинал: ");
        if (!int.TryParse(Console.ReadLine(), out int d)) return;
        Console.Write("Количество: ");
        if (!int.TryParse(Console.ReadLine(), out int c)) return;
        vm.InsertCoin(d, c);
        Console.WriteLine($"Баланс: {vm.Balance}р");
    }

    static void Buy(VendingMachine vm)
    {
        ShowProducts(vm);
        Console.Write("Введите id товара: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;
        var ok = vm.Purchase(id, out var msg, out var change);
        Console.WriteLine(msg);
        if (ok && change != null && change.Count > 0)
        {
            var parts = change.OrderByDescending(k => k.Key).Select(kv => $"{kv.Key}р*{kv.Value}");
            Console.WriteLine("Сдача: " + string.Join(", ", parts));
        }
    }

    static void Cancel(VendingMachine vm)
    {
        var back = vm.Cancel();
        if (back.Count == 0) { Console.WriteLine("Возвращать нечего."); return; }
        var parts = back.OrderByDescending(k => k.Key).Select(kv => $"{kv.Key}р*{kv.Value}");
        Console.WriteLine("Возврат: " + string.Join(", ", parts));
    }

    static void Admin(VendingMachine vm)
    {
        Console.Write("PIN: ");
        var pin = Console.ReadLine();
        if (pin != "1234") { Console.WriteLine("Доступ запрещён."); return; }
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Добавить товар");
            Console.WriteLine("2. Пополнить товар");
            Console.WriteLine("3. Изменить цену");
            Console.WriteLine("4. Состояние монет");
            Console.WriteLine("5. Забрать деньги");
            Console.WriteLine("0. Выход");
            Console.Write("Выбор: ");
            var ch = Console.ReadLine();
            if (ch == "0") break;
            if (ch == "1")
            {
                Console.Write("Название: ");
                var name = Console.ReadLine();
                Console.Write("Цена (р): ");
                if (!int.TryParse(Console.ReadLine(), out int price)) continue;
                Console.Write("Количество: ");
                if (!int.TryParse(Console.ReadLine(), out int qty)) continue;
                vm.AddProduct(name, price, qty);
                Console.WriteLine("Добавлено.");
            }
            else if (ch == "2")
            {
                ShowProducts(vm);
                Console.Write("ID: ");
                if (!int.TryParse(Console.ReadLine(), out int id)) continue;
                Console.Write("Добавить штук: ");
                if (!int.TryParse(Console.ReadLine(), out int add)) continue;
                Console.WriteLine(vm.Restock(id, add) ? "Готово." : "Ошибка.");
            }
            else if (ch == "3")
            {
                ShowProducts(vm);
                Console.Write("ID: ");
                if (!int.TryParse(Console.ReadLine(), out int id)) continue;
                Console.Write("Новая цена: ");
                if (!int.TryParse(Console.ReadLine(), out int price)) continue;
                Console.WriteLine(vm.Reprice(id, price) ? "Готово." : "Ошибка.");
            }
            else if (ch == "4")
            {
                Console.WriteLine(vm.BankInfo());
            }
            else if (ch == "5")
            {
                int total = vm.collectCash();
            }
        }
    }
}

static class Extensions
{
    public static int collectCash(this VendingMachine vm)
    {
        int t = vm.CollectCash();
        Console.WriteLine($"Выдано администратору: {t}р");
        return t;
    }
}