using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SistemaInventario
{
    class Producto { public int Id; public string Nombre; public decimal Precio; }
    class Inventario { public Producto Producto; public string Talla, Color; public int Cantidad, StockMinimo; }

    class Program
    {
        static List<Producto> productos = new();
        static List<Inventario> inventarios = new();
        static readonly ReaderWriterLockSlim rwLock = new();
        static readonly object discoLk = new();
        const string LOG = "log.txt";
        const string PROD = "productos.txt";
        const string INV = "inventario.txt";
        const string VENT = "ventas.txt";
        const string CFG = "config.txt";

        static bool modoDisco = false;
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (!File.Exists(CFG)) File.WriteAllText(CFG, "MODO=RAM\n# Cambie a MODO=DISCO para escritura en disco durante ventas");
            modoDisco = File.ReadAllText(CFG).ToUpper().Contains("DISCO");

            File.WriteAllText(LOG, $"=== INICIO {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Modo: {(modoDisco ? "DISCO" : "RAM")} ===\n");
            CargarDesdeDisco();

            int op;
            do
            {
                Console.Clear();
                Console.WriteLine("=== INVENTARIO CONCURRENTE ===");
                Console.WriteLine($"Modo: {(modoDisco ? "DISCO" : "RAM")}");
                Console.WriteLine("1. Registrar producto");
                Console.WriteLine("2. Agregar inventario");
                Console.WriteLine("3. Simular ventas concurrentes");
                Console.WriteLine("4. Ver inventario");
                Console.WriteLine("5. Benchmark RAM vs Disco");
                Console.WriteLine("6. Ver archivos .txt");
                Console.WriteLine("0. Salir");
                Console.Write("Opción: ");

                if (!int.TryParse(Console.ReadLine(), out op)) continue;

                switch (op)
                {
                    case 1: RegistrarProducto(); break;
                    case 2: AgregarInventario(); break;
                    case 3: SimularVentas(); break;
                    case 4: VerInventario(); break;
                    case 5: Benchmark(); break;
                    case 6: VerArchivos(); break;
                }

                if (op != 0) { Console.Write("\nEnter para continuar..."); Console.ReadLine(); }

            } while (op != 0);

            GuardarEnDisco();
            Log("Sistema cerrado.");
        }
        static void Log(string msg)
        {
            string linea = $"{DateTime.Now:HH:mm:ss.fff} [H{Thread.CurrentThread.ManagedThreadId}] {msg}";
            lock (discoLk) File.AppendAllText(LOG, linea + "\n");
            Console.WriteLine("  " + linea);
        }
        static void CargarDesdeDisco()
        {
            var sw = Stopwatch.StartNew();

            if (File.Exists(PROD))
                foreach (var l in File.ReadAllLines(PROD).Where(l => !l.StartsWith("#")))
                {
                    var p = l.Split('|');
                    if (p.Length == 3 && int.TryParse(p[0], out int id) && decimal.TryParse(p[2], out decimal pr))
                        productos.Add(new Producto { Id = id, Nombre = p[1], Precio = pr });
                }

            if (File.Exists(INV))
                foreach (var l in File.ReadAllLines(INV).Where(l => !l.StartsWith("#")))
                {
                    var p = l.Split('|');
                    if (p.Length == 5 && int.TryParse(p[0], out int pId)
                        && int.TryParse(p[3], out int cant) && int.TryParse(p[4], out int sm))
                    {
                        var prod = productos.FirstOrDefault(x => x.Id == pId);
                        if (prod != null)
                            inventarios.Add(new Inventario { Producto = prod, Talla = p[1], Color = p[2], Cantidad = cant, StockMinimo = sm });
                    }
                }

            Log($"Cargado desde disco en {sw.ElapsedMilliseconds} ms ({productos.Count} productos, {inventarios.Count} ítems)");
        }
        static void GuardarEnDisco()
        {
            rwLock.EnterReadLock();
            try
            {
                var sbP = new StringBuilder("# id|nombre|precio\n");
                foreach (var p in productos) sbP.AppendLine($"{p.Id}|{p.Nombre}|{p.Precio}");
                lock (discoLk) File.WriteAllText(PROD, sbP.ToString());

                var sbI = new StringBuilder("# prodId|talla|color|cantidad|stockMin\n");
                foreach (var i in inventarios) sbI.AppendLine($"{i.Producto.Id}|{i.Talla}|{i.Color}|{i.Cantidad}|{i.StockMinimo}");
                lock (discoLk) File.WriteAllText(INV, sbI.ToString());
            }
            finally { rwLock.ExitReadLock(); }
        }
        static void RegistrarProducto()
        {
            Console.Write("ID: "); if (!int.TryParse(Console.ReadLine(), out int id)) return;

            rwLock.EnterReadLock();
            bool existe = productos.Any(p => p.Id == id);
            rwLock.ExitReadLock();
            if (existe) { Console.WriteLine("❌ ID ya existe."); return; }

            Console.Write("Nombre: "); string nombre = Console.ReadLine();
            Console.Write("Precio: "); if (!decimal.TryParse(Console.ReadLine(), out decimal precio)) return;

            rwLock.EnterWriteLock();
            productos.Add(new Producto { Id = id, Nombre = nombre, Precio = precio });
            rwLock.ExitWriteLock();

            GuardarEnDisco();
            Log($"Producto registrado: {nombre}");
        }
        static void AgregarInventario()
        {
            Console.Write("ID Producto: "); if (!int.TryParse(Console.ReadLine(), out int id)) return;

            rwLock.EnterReadLock();
            var prod = productos.FirstOrDefault(p => p.Id == id);
            rwLock.ExitReadLock();
            if (prod == null) { Console.WriteLine("❌ Producto no existe."); return; }

            Console.Write("Talla: "); string talla = Console.ReadLine();
            Console.Write("Color: "); string color = Console.ReadLine();
            Console.Write("Cantidad: "); if (!int.TryParse(Console.ReadLine(), out int cant)) return;
            Console.Write("Stock mínimo: "); if (!int.TryParse(Console.ReadLine(), out int sm)) return;

            rwLock.EnterWriteLock();
            var existente = inventarios.FirstOrDefault(i => i.Producto.Id == id && i.Talla == talla && i.Color == color);
            if (existente != null) existente.Cantidad += cant;
            else inventarios.Add(new Inventario { Producto = prod, Talla = talla, Color = color, Cantidad = cant, StockMinimo = sm });
            rwLock.ExitWriteLock();

            GuardarEnDisco();
            Log($"Inventario: {prod.Nombre} {talla}/{color} +{cant}");
        }
        static void SimularVentas()
        {
            rwLock.EnterReadLock();
            bool hayItems = inventarios.Count > 0;
            rwLock.ExitReadLock();

            if (!hayItems)
            {
                Console.WriteLine("Sin inventario. Cargando datos de demo...");
                rwLock.EnterWriteLock();
                var p1 = new Producto { Id = 1, Nombre = "Camiseta", Precio = 45000 };
                var p2 = new Producto { Id = 2, Nombre = "Jean", Precio = 89000 };
                productos.AddRange(new[] { p1, p2 });
                inventarios.AddRange(new[] {
                    new Inventario { Producto = p1, Talla = "M",  Color = "Blanco", Cantidad = 20, StockMinimo = 3 },
                    new Inventario { Producto = p1, Talla = "L",  Color = "Negro",  Cantidad = 15, StockMinimo = 3 },
                    new Inventario { Producto = p2, Talla = "32", Color = "Azul",   Cantidad = 12, StockMinimo = 2 },
                });
                rwLock.ExitWriteLock();
                GuardarEnDisco();
            }

            Console.Write("Número de hilos (vendedores): ");
            if (!int.TryParse(Console.ReadLine(), out int nHilos) || nHilos < 1) nHilos = 10;

            lock (discoLk) File.WriteAllText(VENT, $"# Ventas {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n# producto|talla|color|cantidad|hilo\n");

            int exitosas = 0, fallidas = 0;
            var sw = Stopwatch.StartNew();
            using var barrera = new Barrier(nHilos);

            var tareas = Enumerable.Range(1, nHilos).Select(hiloId => Task.Run(() =>
            {
                barrera.SignalAndWait(); 
                var rnd = new Random(Guid.NewGuid().GetHashCode());

                for (int v = 0; v < rnd.Next(3, 7); v++)
                {
                    rwLock.EnterReadLock();
                    var item = inventarios.Count > 0 ? inventarios[rnd.Next(inventarios.Count)] : null;
                    rwLock.ExitReadLock();
                    if (item == null) break;
                    int cantVenta = rnd.Next(1, 4);
                    bool ok = false;
                    rwLock.EnterWriteLock();
                    if (item.Cantidad >= cantVenta) { item.Cantidad -= cantVenta; ok = true; }
                    rwLock.ExitWriteLock();

                    if (ok)
                    {
                        Interlocked.Increment(ref exitosas);
                        Log($"VENTA OK  | H{hiloId} | {item.Producto.Nombre} {item.Talla}/{item.Color} x{cantVenta} | stock={item.Cantidad}");
                        if (modoDisco)
                            lock (discoLk) { File.AppendAllText(VENT, $"{item.Producto.Nombre}|{item.Talla}|{item.Color}|{cantVenta}|H{hiloId}\n"); Thread.Sleep(5); }
                    }
                    else
                    {
                        Interlocked.Increment(ref fallidas);
                        Log($"SIN STOCK | H{hiloId} | {item.Producto.Nombre} necesita {cantVenta}, hay {item.Cantidad}");
                    }

                    Thread.Sleep(rnd.Next(10, 40));
                }
            })).ToArray();

            Task.WaitAll(tareas);
            sw.Stop();
            GuardarEnDisco();

            Console.WriteLine($"\n  Hilos: {nHilos} | Exitosas: {exitosas} | Fallidas: {fallidas} | Tiempo: {sw.ElapsedMilliseconds} ms");
            Log($"Simulación: {exitosas} ventas, {fallidas} fallidas, {sw.ElapsedMilliseconds} ms");
        }
        static void VerInventario()
        {
            rwLock.EnterReadLock();
            var copia = inventarios.ToList();
            rwLock.ExitReadLock();

            if (!copia.Any()) { Console.WriteLine("Sin inventario."); return; }

            foreach (var g in copia.GroupBy(i => i.Producto.Nombre))
            {
                Console.WriteLine($"\n  {g.Key}");
                foreach (var i in g)
                    Console.WriteLine($"    {i.Talla}/{i.Color}  Cant:{i.Cantidad}{(i.Cantidad <= i.StockMinimo ? " ⚠ BAJO" : "")}");
            }
        }
        static void Benchmark()
        {
            int n = 500;
            string tmp = "bench_tmp.txt";
            var buf = new List<string>();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++) buf.Add($"dato-{i}");
            long tRAM = sw.ElapsedMilliseconds;

            sw.Restart();
            for (int i = 0; i < n; i++) File.AppendAllText(tmp, $"dato-{i}\n");
            long tDisco = sw.ElapsedMilliseconds;
            if (File.Exists(tmp)) File.Delete(tmp);

            Console.WriteLine($"\n  {n} escrituras → RAM: {tRAM} ms | Disco: {tDisco} ms");
            Console.WriteLine($"  Conclusión: RAM es ~{(tDisco > 0 ? (double)tDisco / Math.Max(tRAM, 1) : tDisco):F1}x más rápida en operaciones concurrentes");
            Log($"Benchmark: RAM={tRAM}ms | Disco={tDisco}ms");
        }
        static void VerArchivos()
        {
            foreach (var (archivo, titulo) in new[] { (PROD, "PRODUCTOS"), (INV, "INVENTARIO"), (VENT, "VENTAS"), (CFG, "CONFIG") })
            {
                Console.WriteLine($"\n--- {titulo} ({archivo}) ---");
                Console.WriteLine(File.Exists(archivo) ? File.ReadAllText(archivo) : "(vacío)");
            }
        }
    }
}
