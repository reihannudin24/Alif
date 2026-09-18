using System.Collections.Generic;

namespace Alif.Systems
{
    /// <summary>
    /// Keterangan barang untuk kartu detail di Tas: kegunaan di dalam cerita dan fun fact
    /// literasi keuangan. Dicari dari nama barang (sama dengan ItemPickup._itemName), jadi
    /// scene tidak perlu diubah saat teksnya disunting.
    /// </summary>
    public static class ItemCatalog
    {
        public readonly struct ItemInfo
        {
            public readonly string Usage;
            public readonly string FunFact;

            public ItemInfo(string usage, string funFact)
            {
                Usage = usage;
                FunFact = funFact;
            }
        }

        private static readonly Dictionary<string, ItemInfo> Items = new Dictionary<string, ItemInfo>
        {
            ["Voucher Promo Kereta"] = new ItemInfo(
                "Memberi potongan harga tiket kereta untuk perjalanan berikutnya. Tunjukkan ke petugas loket sebelum membayar.",
                "Voucher adalah diskon, bukan uang tunai: nilainya hanya berlaku untuk produk, waktu, dan syarat tertentu. Cek masa berlaku dan ketentuannya dulu — membeli hanya karena ada promo justru bisa menambah pengeluaran."),
        };

        private static readonly ItemInfo Unknown = new ItemInfo(
            "Barang bawaan Alif.",
            "Belum ada catatan tentang barang ini.");

        public static ItemInfo Get(string itemName) =>
            itemName != null && Items.TryGetValue(itemName, out ItemInfo info) ? info : Unknown;
    }
}
