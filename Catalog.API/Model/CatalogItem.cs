using Microsoft.AspNetCore.Http.Connections;
using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Model
{
    public class CatalogItem
    {
        [Key]
        public int Id
        {
            get;
            set;
        }
        [Required]
        [StringLength(50)]
        public string Name
        {
            get;
            set;
        }
        public string Descripcion { 
            get;
            set;
        }
        [Required]
        public decimal Price
        {
            get;
            set;
        }
        public CatalogType CatalogType
        {
            get;
            set;
        }
        public CatalogBrand CatalogBrand
        {
            get;
            set;
        }
        public int AvailableStock
        {
            get;
            set;
        }
        public int RestockThreshold
        {
            get;
            set;
        }
        public int MaxStockThreshold
        {
            get;
            set;
        }
        public bool OnReorder
        {
            get;
            set;
        }
        public CatalogItem() { }
    }
}
