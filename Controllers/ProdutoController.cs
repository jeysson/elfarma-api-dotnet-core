using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]    
    [Route("api/[controller]")]
    public class ProdutoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProdutoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("todos")]
        public IEnumerable<Produto> Todos(int loja)
        {
            var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja);
            //
            var dados = _context.Produtos.Include(p => p.Loja).Include(p=> p.GrupoProdutos)
                .ThenInclude(p=> p.Grupo)
                .Where(p => p.GrupoProdutos.Count > 0 && p.Ativo && p.Loja.Id == loja).ToList();
            //
            dados.ForEach(p =>
            {
                p.Loja.Banner = null;
                p.Loja.Logo = null;
                p.Loja.ImgBanner = null;
                p.Loja.ImgLogo = null;
                //
                if (p.Loja.IncluirComissao)
                    p.Preco = (1 + tarifa.Comissao) * p.Preco;
            });

            return dados;
        }

        [HttpGet("grupos")]
        public IEnumerable<dynamic> Grupo(int loja)
        {
            var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja);
            //
            var grupos = _context.Grupos.Include(p => p.GrupoProdutos)
                .ThenInclude(p => p.Produto)
                .ThenInclude(p=> p.Loja)
                .Where(p => p.GrupoProdutos.Count(p=> p.Produto.Ativo) > 0 && p.Loja.Id == loja && p.Ativo)
                .Select(p => new
                {
                    Id = p.Id
                ,
                    Nome = p.Nome
                ,
                    Ordem = p.Ordem
                ,
                    Products = p.GrupoProdutos.Where(p=> p.Produto.Ativo).Select(q => q.Produto).ToList()
                }).ToList();            
            //
            foreach (var produto in grupos.SelectMany(p => p.Products).Distinct())
            {
                produto.Loja.ImgLogo = null;
                produto.Loja.ImgBanner = null;
                produto.Loja.Logo = null;
                produto.Loja.Banner = null;
                //
                if (produto.Loja.IncluirComissao)
                    produto.Preco = (1 + tarifa.Comissao) * produto.Preco;
            }

            return grupos;
        }

        [HttpGet("produtosgrupo")]
        public IEnumerable<Produto> ProdutosGrupo(int loja, int grupo)
        {
            var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja);
            //
            var dados = _context.Produtos.Include(p=> p.Loja).Include(p => p.GrupoProdutos)
                .ThenInclude(p => p.Grupo)
                .Where(p => p.GrupoProdutos.Where(p=> p.Produto.Ativo)
                                           .Count(z=> z.GrupoId == grupo) > 0 && p.Loja.Id == loja).ToList();
            //
            dados.ForEach(p =>
            {
                p.Loja.Banner = null;
                p.Loja.Logo = null;
                p.Loja.ImgBanner = null;
                p.Loja.ImgLogo = null;
                //
                if (p.Loja.IncluirComissao)
                    p.Preco = (1 + tarifa.Comissao) * p.Preco;
            });

            return dados;
        }

        [HttpGet("imagens")]
        public IEnumerable<ProdutoFoto> Imagens(int grupo)
        {
            var list = _context.ProdutoFotos.Include(p=> p.Produto)
                                            .Where(p => p.Produto.GrupoProdutos.Where(p=> p.Produto.Ativo).First().GrupoId == grupo).ToList();
           
            return list;
        }

        [HttpGet("paginar")]
        public async Task<List<Produto>> Paginar(int loja, int grupo, int indice, int tamanho)
        {
            Paginar<Produto> produtos;
            //
            var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja);
            //
            if (grupo == -1)
                produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Include(p=> p.Loja).Where(p => p.Loja.Id == loja && p.Ativo), indice, tamanho);
            else
            produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Include(p => p.Loja).Where(p=> p.Ativo && p.Loja.Id == loja && p.GrupoProdutos.Count(p=> p.GrupoId == grupo )> 0),  indice, tamanho);

            //
            produtos.Itens.ForEach(p =>
            {
                p.Loja.Banner = null;
                p.Loja.Logo = null;
                p.Loja.ImgBanner = null;
                p.Loja.ImgLogo = null;
                //
                if (p.Loja.IncluirComissao)
                    p.Preco = (1 + tarifa.Comissao) * p.Preco;
            });

            return produtos.Itens;
        }

        [HttpGet("buscarporloja")]
        public async Task<List<Produto>> BuscarPorLoja(int loja, string nomeproduto, int indice, int tamanho)
        {
            Paginar<Produto> produtos;
            //
            var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja);
            //
            if (!string.IsNullOrEmpty(nomeproduto))
            {
                produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Loja.Id == loja &&
                (p.Nome.ToUpper().Contains(nomeproduto.ToUpper()) || p.Descricao.ToUpper().Contains(nomeproduto.ToUpper()))).Include(p=> p.ProdutoFotos).Include(p=> p.Loja), indice, tamanho);
            }
            else {
                produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Loja.Id == loja).Include(p=> p.ProdutoFotos).Include(p => p.Loja), indice, tamanho);
            }

            produtos.Itens.ForEach(o => {
                o.Loja.Banner = null;
                o.Loja.Logo = null;
                o.Loja.ImgBanner = null;
                o.Loja.ImgLogo = null;
                //
                if (o.Loja.IncluirComissao)
                    o.Preco = (1 + tarifa.Comissao) * o.Preco;
            });           
            //
            return produtos.Itens;
        }
                
        [HttpGet("buscar")]
        public async Task<List<Produto>> Buscar(string nomeproduto, int indice, int tamanho)
        {           
            
            var produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Ativo && p.Nome.ToUpper().Contains(nomeproduto.ToUpper()) ||
             p.Descricao.ToUpper().Contains(nomeproduto.ToUpper()))
                .Include(p => p.Loja).Include(p => p.ProdutoFotos).OrderBy(p => p.Preco), indice, tamanho);
            //
            var lojas = produtos.Itens.GroupBy(p => p.Loja).Select(p=> p.Key);
            //
            foreach (var loja in lojas)
            {
                var tarifa = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja.Id);
                //
                foreach (var produto in produtos.Itens.Where(p => p.Loja.Id == loja.Id))
                {
                    produto.Preco = (1 + tarifa.Comissao) * produto.Preco;
                }
            }
            //
            return produtos.Itens;
        }

        [HttpGet("imagensgrupo")]
        public IActionResult ImagensGrupo(int grupo)
        {
            var list = _context.GrupoProdutos
                                .Include(p => p.Produto)
                                .ThenInclude(p=> p.ProdutoFotos)
                                .Where(p => p.Produto.Ativo && p.GrupoId == grupo)
                                .SelectMany(p=> p.Produto.ProdutoFotos).ToList();
            //
            return Ok(list);
        }

        [HttpGet("paginarproduto")]
        public async Task<IActionResult> PaginarProduto(uint loja, int pagina, int registrosPagina, string filtro = "") 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                if (string.IsNullOrEmpty(filtro))
                {
                    mensageiro.Dados = await Paginar<Produto>.CreateAsync(_context.Produtos.Include(p => p.Loja).Include(p => p.Categoria).Include(p => p.Marca)
                        .Include(p => p.UnidadeMedida)
                        .Where(p => p.LojaId == loja).AsNoTracking()
                        .OrderBy(p => p.Id), pagina, registrosPagina);
                }
                else
                {
                    mensageiro.Dados = await Paginar<Produto>.CreateAsync(_context.Produtos.Include(p => p.Loja).Include(p => p.Categoria).Include(p => p.Marca).Include(p => p.UnidadeMedida)
                        .Where(p => p.Nome.ToUpper().Contains(filtro.ToUpper()) && p.Loja.Id == loja).AsNoTracking()
                        .OrderBy(p => p.Nome), pagina, registrosPagina);
                }
            }

            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("pagingproduto")]
        public async Task<IActionResult> PagingProduto(uint loja, int pagina, int registrosPagina, string filtro = "")
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                if (string.IsNullOrEmpty(filtro))
                {
                    mensageiro.Dados = await Paging<Produto>.CreateAsync(_context.Produtos.Include(p => p.Loja).Include(p => p.Categoria).Include(p => p.Marca)
                        .Include(p => p.UnidadeMedida)
                        .Where(p => p.LojaId == loja).AsNoTracking()
                        .OrderBy(p => p.Id), pagina, registrosPagina);
                }
                else
                {
                    mensageiro.Dados = await Paging<Produto>.CreateAsync(_context.Produtos.Include(p => p.Loja).Include(p => p.Categoria).Include(p => p.Marca)
                        .Include(p => p.UnidadeMedida)
                        .Where(p => p.Nome.ToUpper().Contains(filtro.ToUpper()) && p.Loja.Id == loja).AsNoTracking()
                        .OrderBy(p => p.Nome), pagina, registrosPagina);
                }
            }

            catch (Exception ex)
            {
                return null;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterfotos")]
        public async Task<IActionResult> ObterProdutoFotos(uint id)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                mensageiro.Dados = _context.ProdutoFotos.Where(p => p.ProdutoId == id).AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        #region Operações pela loja
        [HttpGet("buscarporloja2")]
        public async Task<Mensageiro> BuscarPorLoja2(int loja, string nomeproduto, int indice, int tamanho)
        {
            Mensageiro meng = new Mensageiro();
            meng.Codigo = 200;
            meng.Mensagem = "Operação realizada com sucesso!";
            try
            {
                Paginar<Produto> produtos;

                if (!string.IsNullOrEmpty(nomeproduto))
                {
                    produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Loja.Id == loja &&
                    (p.Nome.ToUpper().Contains(nomeproduto.ToUpper()) || p.Descricao.ToUpper().Contains(nomeproduto.ToUpper())))
                        .Include(p => p.Categoria).Include(p => p.Marca).Include(p => p.UnidadeMedida), indice, tamanho);
                }
                else
                {
                    produtos = await Paginar<Produto>.CreateAsync(_context.Produtos.Where(p => p.Loja.Id == loja)
                        .Include(p => p.Categoria).Include(p => p.Marca).Include(p => p.UnidadeMedida), indice, tamanho);
                }

                meng.Dados = produtos;
            }
            catch (Exception ex)
            {
                meng.Codigo = 300;
                meng.Mensagem = "Ocorreu uma falha ao buscar os produtos!";
            }
            return meng;
        }


        [HttpPost("cadastrarproduto")]
        public async Task<IActionResult> CadastrarProduto(Produto produto)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();

                produto.LojaId = produto.Loja.Id;
                produto.Loja = null;
                produto.MarcaId = produto.Marca.Id;
                produto.Marca = null;
                produto.UnidadeMedidaId = produto.UnidadeMedida.Id;
                produto.UnidadeMedida = null;
                produto.CategoriaId = produto.Categoria.Id;
                produto.Categoria = null;

                _context.Produtos.Add(produto);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpPut("atualizarproduto")]
        public async Task<IActionResult> AtualizarProduto(Produto produto)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();
                //
                _context.ProdutoFotos.RemoveRange(_context.ProdutoFotos.Where(p => p.ProdutoId == produto.Id));
                _context.ProdutoFotos.AddRange(produto.ProdutoFotos);
                //                
                _context.Entry(produto).Property(p => p.MarcaId).IsModified = true;
                _context.Entry(produto).Property(p => p.UnidadeMedidaId).IsModified = true;
                _context.Entry(produto).Property(p => p.CategoriaId).IsModified = true;
                _context.Entry(produto).Property(p => p.Nome).IsModified = true;
                _context.Entry(produto).Property(p => p.Descricao).IsModified = true;
                _context.Entry(produto).Property(p => p.Preco).IsModified = true;
                _context.Entry(produto).Property(p => p.PrecoPromocional).IsModified = true;
                _context.Entry(produto).Property(p => p.Ativo).IsModified = true;
                _context.Entry(produto).Property(p => p.RetemReceita).IsModified = true;
                //
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpPut("inativarproduto")]
        public async Task<IActionResult> InativarProduto(Produto produto)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();
                //                
                _context.Entry(produto).Property(p => p.Ativo).IsModified = true;
                //
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluirproduto")]
        public async Task<IActionResult> ExcluirProduto(int id)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();
                //
                _context.ProdutoFotos.RemoveRange(_context.ProdutoFotos.Where(p => p.ProdutoId == id).AsNoTracking());
                _context.Produtos.Remove(_context.Produtos.FirstOrDefault(p=> p.Id == id));
                _context.SaveChanges();
                //
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }
        #endregion


       
    }
}
