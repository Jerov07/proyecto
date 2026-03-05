using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;



namespace SistemaInventarioTienda
{
    class Program
    {
        static List<Producto> productos = new List<Producto>();
        static List<Inventario> inventarios = new List<Inventario>();

        static void Main(string[] args)
        {
            int opcion;

            do
            {
                Console.Clear();
                Console.WriteLine("=====================================");
                Console.WriteLine(" SISTEMA DE INVENTARIO – TIENDA ROPA ");
                Console.WriteLine("=====================================");
                Console.WriteLine("1. Registrar Producto");
                Console.WriteLine("2. Agregar Inventario");
                Console.WriteLine("3. Registrar Venta");
                Console.WriteLine("4. Ver Inventario");
                Console.WriteLine("5. Salir");
                Console.Write("Seleccione una opción: ");

                opcion = int.Parse(Console.ReadLine());

                switch (opcion)
                {
                    case 1:
                        RegistrarProducto();
                        break;

                    case 2:
                        AgregarInventario();
                        break;

                    case 3:
                        RegistrarVenta();
                        break;

                    case 4:
                        VerInventario();
                        break;
                }

                Console.WriteLine("\nPresione una tecla para continuar...");
                Console.ReadKey();

            } while (opcion != 5);
        }

        static void RegistrarProducto()
        {
            Console.Clear();
            Console.WriteLine("----- REGISTRAR PRODUCTO -----");

            Console.Write("ID: ");
            int id = int.Parse(Console.ReadLine());

            Console.Write("Nombre: ");
            string nombre = Console.ReadLine();

            Console.Write("Precio Venta: ");
            decimal precio = decimal.Parse(Console.ReadLine());

            productos.Add(new Producto { Id = id, Nombre = nombre, Precio = precio });

            Console.WriteLine("✔ Producto registrado correctamente.");
        }

        static void AgregarInventario()
        {
            Console.Clear();
            Console.WriteLine("----- AGREGAR INVENTARIO -----");

            Console.Write("ID Producto: ");
            int id = int.Parse(Console.ReadLine());

            var producto = productos.FirstOrDefault(p => p.Id == id);

            if (producto == null)
            {
                Console.WriteLine("Producto no encontrado.");
                return;
            }

            Console.Write("Talla: ");
            string talla = Console.ReadLine();

            Console.Write("Color: ");
            string color = Console.ReadLine();

            Console.Write("Cantidad: ");
            int cantidad = int.Parse(Console.ReadLine());

            Console.Write("Stock Mínimo: ");
            int stockMin = int.Parse(Console.ReadLine());

            inventarios.Add(new Inventario
            {
                Producto = producto,
                Talla = talla,
                Color = color,
                Cantidad = cantidad,
                StockMinimo = stockMin
            });

            Console.WriteLine("✔ Inventario agregado correctamente.");
        }

        static void RegistrarVenta()
        {
            Console.Clear();
            Console.WriteLine("----- REGISTRAR VENTA -----");

            Console.Write("ID Producto: ");
            int id = int.Parse(Console.ReadLine());

            Console.Write("Talla: ");
            string talla = Console.ReadLine();

            Console.Write("Color: ");
            string color = Console.ReadLine();

            var item = inventarios.FirstOrDefault(i =>
                i.Producto.Id == id &&
                i.Talla == talla &&
                i.Color == color);

            if (item == null)
            {
                Console.WriteLine("Producto no encontrado en inventario.");
                return;
            }

            Console.Write("Cantidad a vender: ");
            int cantidad = int.Parse(Console.ReadLine());

            if (item.Cantidad < cantidad)
            {
                Console.WriteLine("Stock insuficiente.");
                return;
            }

            item.Cantidad -= cantidad;

            decimal total = cantidad * item.Producto.Precio;

            Console.WriteLine("✔ Venta realizada con éxito.");
            Console.WriteLine($"Total a pagar: ${total}");

            if (item.Cantidad <= item.StockMinimo)
            {
                Console.WriteLine("ALERTA: Producto en bajo stock.");
            }
        }

        static void VerInventario()
        {
            Console.Clear();
            Console.WriteLine("----- INVENTARIO ACTUAL -----");

            foreach (var item in inventarios)
            {
                Console.WriteLine($"Producto: {item.Producto.Nombre}");
                Console.WriteLine($"Talla: {item.Talla}");
                Console.WriteLine($"Color: {item.Color}");
                Console.WriteLine($"Cantidad: {item.Cantidad}");
                Console.WriteLine("---------------------------");
            }
        }
    }

    class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
    }

    class Inventario
    {
        public Producto Producto { get; set; }
        public string Talla { get; set; }
        public string Color { get; set; }
        public int Cantidad { get; set; }
        public int StockMinimo { get; set; }
    }
}
