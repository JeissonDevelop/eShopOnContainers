﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data
{
    public class CatalogContext: DbContext
    {
        public DbSet<CatalogBrand> CatalogBrands
        {
            get;
            set;
        }
        public DbSet<CatalogType> CatalogTypes
        {
            get;
            set;
        }
        public DbSet<CatalogItem> CatalogItems
        {
            get;
            set;
        }
        public CatalogContext(DbContextOptions<CatalogContext> options): base(options) {  }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<CatalogItem>().Property(ci => ci.Price).HasColumnType("decimal(5,3)");
            builder.Entity<CatalogItem>().HasAlternateKey(ci => ci.Name);
        }
    }
}
