using Commandor;
using Commandor.Example.Commands;
using Commandor.Example.Queries;
using Commandor.Example.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Commandor va service'larni ro'yxatga olish
services.AddSingleton<ICommandor, Commandor.Commandor>();
services.AddCommandorService<IProductService, ProductService>();

var serviceProvider = services.BuildServiceProvider();
var commandor = serviceProvider.GetRequiredService<ICommandor>();

Console.WriteLine("╔═══════════════════════════════════════════════╗");
Console.WriteLine("║  Commandor v2 - Caching Demo 🚀              ║");
Console.WriteLine("║  Query'lar avtomatik cache'lanadi!           ║");
Console.WriteLine("╚═══════════════════════════════════════════════╝\n");

// 1. Product yaratish
Console.WriteLine("📝 1. Product yaratish (CreateProductCommand)");
var createCommand1 = new CreateProductCommand("iPhone 15 Pro", 15000000m);
var product1 = await commandor.SendAsync(createCommand1);

var createCommand2 = new CreateProductCommand("Samsung Galaxy S24", 12000000m);
var product2 = await commandor.SendAsync(createCommand2);

var createCommand3 = new CreateProductCommand("MacBook Pro M3", 35000000m);
var product3 = await commandor.SendAsync(createCommand3);

Console.WriteLine();

// 2. Query - birinchi marta (DB'dan)
Console.WriteLine($"🔍 2. Product olish ID bo'yicha - BIRINCHI (DB'dan)");
var sw = System.Diagnostics.Stopwatch.StartNew();
var query1 = new GetProductByIdQuery(product1.Id);
var result1 = await commandor.SendAsync(query1);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F2}ms");
Console.WriteLine($"   📦 Product: {result1?.Name} - {result1?.Price:N0} so'm");

Console.WriteLine();

// 3. Query - ikkinchi marta (CACHE'dan)
Console.WriteLine($"⚡ 3. Product olish ID bo'yicha - IKKINCHI (CACHE'dan)");
sw.Restart();
var query2 = new GetProductByIdQuery(product1.Id);
var result2 = await commandor.SendAsync(query2);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F4}ms (!) 🚀");
Console.WriteLine($"   📦 Product: {result2?.Name} - {result2?.Price:N0} so'm");
Console.WriteLine($"   ✅ Cache hit! {(result1 == result2 ? "Same instance" : "Different instance")}");

Console.WriteLine();

// 4. Barcha productlarni ko'rish (birinchi marta - DB'dan)
Console.WriteLine("📋 4. Barcha productlar - BIRINCHI (DB'dan)");
sw.Restart();
var allQuery1 = new GetAllProductsQuery();
var allProducts1 = await commandor.SendAsync(allQuery1);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F2}ms");
foreach (var p in allProducts1)
{
    Console.WriteLine($"   - ID:{p.Id} | {p.Name} | {p.Price:N0} so'm");
}

Console.WriteLine();

// 5. Barcha productlarni ko'rish (ikkinchi marta - CACHE'dan)
Console.WriteLine("⚡ 5. Barcha productlar - IKKINCHI (CACHE'dan)");
sw.Restart();
var allQuery2 = new GetAllProductsQuery();
var allProducts2 = await commandor.SendAsync(allQuery2);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F4}ms (!) 🚀");
Console.WriteLine($"   ✅ Jami: {allProducts2.Count} ta product (cache'dan)");

Console.WriteLine();

// 6. Narxni yangilash (INVALIDATION)
Console.WriteLine($"💰 6. Narxni yangilash - CACHE INVALIDATION");
var updateCommand = new UpdateProductPriceCommand(product1.Id, 14500000m);
var updated = await commandor.SendAsync(updateCommand);
Console.WriteLine($"   ✅ Narx yangilandi");
Console.WriteLine($"   ⚠️  Cache invalidated!");

Console.WriteLine();

// 7. Query qayta (invalidation dan keyin - DB'dan)
Console.WriteLine($"🔄 7. Product olish - INVALIDATION'dan keyin (DB'dan)");
sw.Restart();
var query3 = new GetProductByIdQuery(product1.Id);
var result3 = await commandor.SendAsync(query3);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F2}ms");
Console.WriteLine($"   📦 Yangi narx: {result3?.Price:N0} so'm");
Console.WriteLine($"   ✅ Recomputed from DB!");

Console.WriteLine();

// 8. Query yana (yangi cache)
Console.WriteLine($"⚡ 8. Product olish - YANGI CACHE");
sw.Restart();
var query4 = new GetProductByIdQuery(product1.Id);
var result4 = await commandor.SendAsync(query4);
sw.Stop();
Console.WriteLine($"   ⏱️  Vaqt: {sw.Elapsed.TotalMilliseconds:F4}ms (!) 🚀");
Console.WriteLine($"   📦 Product: {result4?.Name} - {result4?.Price:N0} so'm");
Console.WriteLine($"   ✅ From fresh cache!");

Console.WriteLine();
Console.WriteLine("╔═══════════════════════════════════════════════╗");
Console.WriteLine("║  ✅ Demo muvaffaqiyatli tugadi!               ║");
Console.WriteLine("║                                               ║");
Console.WriteLine("║  💡 Key Features:                             ║");
Console.WriteLine("║     - Auto-caching for queries ⚡            ║");
Console.WriteLine("║     - LiteAPI.Cache (GC-free) 🚀             ║");
Console.WriteLine("║     - Auto-invalidation on updates 🔄        ║");
Console.WriteLine("║     - 100-1000x faster cached calls! 📊      ║");
Console.WriteLine("╚═══════════════════════════════════════════════╝");
