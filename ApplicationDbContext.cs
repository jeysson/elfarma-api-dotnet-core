using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AllDelivery.Lib;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace AllDelivery.Api
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //var lf = new LoggerFactory();
            //lf.AddProvider(new MyLoggerProvider());
            //optionsBuilder.UseLoggerFactory(lf);
            //optionsBuilder.EnableSensitiveDataLogging();
        }

        public DbSet<Loja> Lojas { get; set; }

        public DbSet<LojaTarifa> LojaTarifas { get; set; }

        public DbSet<Produto> Produtos { get; set; }

        public DbSet<Grupo> Grupos { get; set; }

        public DbSet<GrupoProduto> GrupoProdutos { get; set; }

        public DbSet<ProdutoFoto> ProdutoFotos { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<PedidoItem> PedidoItens { get; set; }

        public DbSet<PedidoReceita> PedidoReceitas { get; set; }

        public DbSet<HistoricoPedido> HistoricoPedidos { get; set; }

        public DbSet<PedidoAvaliacao> PedidoAvaliacoes { get; set; }

        public DbSet<FormaPagamento> FormaPagamentos { get; set; }

        public DbSet<LojaFormaPagamento> LojaFormaPagamentos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<StatusPedido> StatusPedidos { get; set; }

        public DbSet<UnidadeMedida> UnidadeMedidas { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Marca> Marcas { get; set; }

        public DbSet<CompromissoFinanceiro> CompromissoFinanceiros { get; set; }

        public DbSet<Horario> Horarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet((string)null, DelegationModes.ApplyToDatabases);

            modelBuilder.Entity<GrupoProduto>()
                .HasKey(c => new { c.GrupoId, c.ProdutoId});

            modelBuilder.Entity<ProdutoFoto>()
                .HasKey(c => new { c.ProdutoId, c.Seq });

            modelBuilder.Entity<LojaFormaPagamento>()
                .HasKey(c => new { c.LojaId, c.FormaPagamentoId });

            modelBuilder.Entity<Pedido>().HasOne(p => p.Status).WithMany();

            modelBuilder.Entity<PedidoAvaliacao>().HasOne(p => p.Pedido).WithMany(p=> p.Avaliacoes);
                

        }
    }
}
