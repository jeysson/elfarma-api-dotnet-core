using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PedidoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("registrar")]
        public IActionResult Registrar(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                pedido.Data = DateTime.UtcNow;
                pedido.StatusPedidoId = 1;
                pedido.Ende = pedido.Endereco.ToString();
                pedido.LojaId = pedido.Loja.Id;
                pedido.Loja = null;
                pedido.FormaPagamentoId = pedido.FormaPagamento.Id;
                pedido.FormaPagamento = null;
                pedido.Location = new Point(pedido.Endereco.Lat, pedido.Endereco.Longi);
                //
                pedido.Itens.ForEach(o =>
                {
                    o.ProdutoId = o.Produto.Id;
                    o.Produto = null;
                });
                //
                _context.Pedidos.Add(pedido);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
               
                //
                mensageiro.Dados = pedido.Id;

            }catch(Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
                mensageiro.Dados = new { message = ex.Message, stack = ex.StackTrace,
                innerMessage = ex.InnerException != null? ex.InnerException.Message: null,
                innerstack = ex.InnerException != null ? ex.InnerException.StackTrace: null
                };
            }

            return Ok(mensageiro);
        }

        [HttpGet("obter")]
        public IActionResult Obter(uint idPedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var pedido = _context.Pedidos.Where(p => p.Id == idPedido)
                     .Include(p => p.Itens).ThenInclude(p => p.Produto)
                                                     .Include(p => p.FormaPagamento)
                                                     .Include(p => p.Loja).FirstOrDefault();
                pedido.HistStatus = _context.HistoricoPedidos.Include(p => p.Status).Where(p => p.Pedido.Id == idPedido)
                                                                .Select(p => new StatusPedido
                                                                {
                                                                    Ativo = p.Status.Ativo,
                                                                    Descricao = p.Status.Descricao,
                                                                    Id = p.Status.Id,
                                                                    Nome = p.Status.Nome,
                                                                    Sequencia = p.Status.Sequencia
                                                                }).OrderByDescending(p=> p.Id).ToList();
                if (pedido.Ende[pedido.Ende.Length - 1] != '}')
                    pedido.Ende = pedido.Ende + "}";
                if (pedido.Ende.Contains("}}"))
                    pedido.Ende = pedido.Ende.Replace("}", "");
                //
                
                pedido.Endereco = Newtonsoft.Json.JsonConvert.DeserializeObject<Endereco>(pedido.Ende.ConvertUnicodeToText());
                mensageiro.Dados = pedido;
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return Ok(mensageiro);
        }

        [HttpGet("obterhistorico")]
        public async Task<Mensageiro> ObterHistorico(uint codUser, int indice, int tamanho)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {              

                var page = await Paginar<Pedido>.CreateAsync(_context.Pedidos
                    .Where(p => p.UsuarioId == codUser)
                     .Include(p => p.Loja)
                    .Include(p => p.Itens)
                    .ThenInclude(p => p.Produto)
                    .Include(p=> p.Avaliacoes)
                    .Include(p=> p.Status)
                    .OrderByDescending(p => p.Data)
                    .ThenBy(p => p.Status.Id)
                    .Select(p => new Pedido
                    {
                        Id = p.Id,
                        Loja = new Loja { NomeFantasia = p.Loja.NomeFantasia, ImgLogo = p.Loja.ImgLogo },
                        Data = p.Data,
                        Itens = p.Itens,
                        Status = p.Status,
                        Avaliacoes = p.Avaliacoes
                    })                    
                    , indice, tamanho);

                Paginar<Object> list = new Paginar<Object>(page.Itens.Select(p => new
                {
                    Id = p.Id,
                    Loja = p.Loja.NomeFantasia,
                    Logo = p.Loja.ImgLogo,
                    Data = p.Data,
                    NomeItem1 = p.Itens[0].Produto.Nome,
                    QuantidadeItem1 = p.Itens[0].Quantidade,
                    NomeItem2 = p.Itens.Count > 1 ? p.Itens[1].Produto.Nome: null,
                    QuantidadeItem2 = p.Itens.Count > 1 ? p.Itens[1].Quantidade: null,
                    Quantidade = p.Itens.Count,
                    Status = p.Status,
                    Avaliacao = p.Avaliacoes.Average(z=> z.NotaLoja),
                    DiasAvaliacao = DateTime.Now.Date.Subtract(p.Data.Value).Days
                }).ToList<object>(), page.Itens.Count, indice, tamanho);

                mensageiro.Dados = list.Itens;
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return mensageiro;
        }

        [HttpGet("obteravaliacaopendente")]
        public async Task<Mensageiro> ObterAvaliacaoPendente(int codUser)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var dt = DateTime.Now.AddDays(-16);

                var list = _context.Pedidos.Include(p => p.Status)
                                            .Include(p => p.Loja)
                                            .Where(p => p.UsuarioId == codUser
                                                              && p.Data.Value.Date > dt.Date
                                                              && p.Status.Sequencia == 5
                                                              && p.Loja.Location != null
                                                              && !_context.PedidoAvaliacoes.Any(q => q.Pedido.Id == p.Id));

                mensageiro.Dados = list;
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha na operação!";
            }

            return mensageiro;
        }

        [HttpGet("obtermes")]
        public async Task<IActionResult> ObterMes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                var pedidos = _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Month == DateTime.Now.Month && p.Status.Id == 7)// somente pedidos entregues
                .AsNoTracking().ToList();
                mensageiro.Dados = pedidos;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message; 
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtersomames")]
        public async Task<IActionResult> ObterSomaMes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Month == DateTime.Now.Month && p.Status.Id == 7)// somente pedidos entregues
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega);
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterdia")]
        public async Task<IActionResult> ObterDia(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.Date && p.Status.Id == 7)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        
        }

        [HttpGet("obtersomadia")]
        public async Task<IActionResult> ObterSomaDia(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.Date && p.Status.Id == 7)
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega);
                mensageiro.Dados = xx;
            }
            catch(Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obter2DA")]
        public async Task<IActionResult> ObterPedidos2DiaAnterior(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesos");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 7)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtersoma2DA")]
        public async Task<IActionResult?> ObterSomaVendas2Dia(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesos");

            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 7)
                .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + p.Loja.TaxaEntrega);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obternovosclientes")]
        public async Task<IActionResult> ObterTotalNovosClientes(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                var usuarios = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 7)
                .GroupBy(p => p.UsuarioId)
                .Select(p => p.Key).ToList();

                var usuariosAntigos = _context.Pedidos.Where(p => p.LojaId == loja &&
                p.Data.Value.Date < DateTime.Now.AddDays(-2).Date &&
                usuarios.Contains(p.UsuarioId) && p.Status.Id == 7)
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();

                mensageiro.Dados = usuarios.Except(usuariosAntigos).Count();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message; 
            }

            return Ok(mensageiro);

        }

        [HttpGet("obtersomanovosclientes")]
        public async Task<IActionResult> ObterSomaNovosClientes(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {
                //busca os pedidos de 2 dias atrás
                var usuarios = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Status.Id == 7)
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();
                //buscar todos os pedidos realizados anteriormente 
                var usuariosAntigos = _context.Pedidos.Where(p => p.LojaId == loja && p.Data.Value.Date < DateTime.Now.AddDays(-2).Date && usuarios.Contains(p.UsuarioId) && p.Status.Id == 7)
                    .GroupBy(p => p.UsuarioId)
                    .Select(p => p.Key).ToList();
                //pega somente os usuários que não fizeram pedidos anteriormente
                var novos = usuarios.Except(usuariosAntigos);

                mensageiro.Dados = _context.Pedidos.Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date == DateTime.Now.AddDays(-2).Date && novos.Contains(p.UsuarioId) && p.Status.Id == 7)
                    .Sum(p => p.Itens.Sum(q => q.Quantidade * q.Preco) + p.Loja.TaxaEntrega);
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obterprodutomaisvendido")]
        public async Task<IActionResult> ObterProdutoMaisVendido(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try
            {
                var grp = _context.PedidoItens
                                   .Include(p => p.Pedido)
                                   .Include(p => p.Produto)
                                   .Where(p => p.Pedido.LojaId == loja && p.Pedido.Data.Value.Date == DateTime.Now.AddDays(-2).Date && p.Pedido.Status.Id == 7)
                                   .ToList()
                                   .GroupBy(p => p.Produto);

                mensageiro.Dados = grp.Select(p => new { Produto = p.Key, Total = p.Sum(x => x.Quantidade) })
                            .OrderByDescending(p => p.Total)
                            .FirstOrDefault().Produto;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtersemana")]
        public async Task<IActionResult> ObterPedidosSemana(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var xx = _context.Pedidos.Include(p => p.Itens)
                .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 7)
                .AsNoTracking().ToList();
                mensageiro.Dados = xx;
            }
            catch (Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obtersomasemana")]
        public async Task<IActionResult> ObterSomaVendasSemana(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {

                var soma = _context.Pedidos.Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 7)
                    .Sum(p => p.Itens.Sum(x => x.Preco * x.Quantidade) + taxa);

                mensageiro.Dados = soma;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterpedido7D")]
        public async Task<IActionResult> ObterPedidosUltimos7Dias(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");

            try 
            {
                decimal[] valores = new decimal[7];

                var dias = _context.Pedidos
                    .Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date && p.Status.Id == 7)
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(p => new { DiaSemana = p.Data.Value.DayOfWeek })
                    .OrderBy(p => p.Key.DiaSemana);



                for (var i = 0; i < 7; i++)
                {
                    var grp = dias.FirstOrDefault(p => (int)p.Key.DiaSemana == i);
                    if (grp != null)
                        valores[i] = grp.Sum(p => p.Itens.Sum(x => x.Quantidade.Value * x.Preco.Value) + taxa);
                    else
                        valores[i] = 0;
                }

                mensageiro.Dados = valores;
            }
            catch(Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterpedido14D")]
        public async Task<IActionResult> ObterPedidos14Dias(uint loja, decimal taxa) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");

            try 
            {
                decimal[] valores = new decimal[7];

                var dias = _context.Pedidos
                    .Include(p => p.Itens)
                    .Where(p => p.LojaId == loja && p.Data.Value.Date <= DateTime.Now.AddDays(-7).Date && p.Data.Value.Date > DateTime.Now.AddDays(-14).Date && p.Status.Id == 7)
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(p => new { DiaSemana = p.Data.Value.DayOfWeek })
                    .OrderBy(p => p.Key.DiaSemana);


                for (var i = 0; i < 7; i++)
                {
                    var grp = dias.FirstOrDefault(p => (int)p.Key.DiaSemana == i);
                    if (grp != null)
                        valores[i] = grp.Sum(p => p.Itens.Sum(x => x.Quantidade.Value * x.Preco.Value) + taxa);
                    else
                        valores[i] = 0;
                }

                mensageiro.Dados = valores;
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("atrasos")]
        public async Task<IActionResult> PorcentagemAtraso(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try 
            {
                var entregasAtrasos = _context.Pedidos.Where(p => p.DataEntrega.Value > p.Data.Value.AddMinutes(p.Loja.TempoMaximo.Value)
                 && p.LojaId == loja && p.Status.Id == 7 && p.Data.Value.Date > DateTime.Now.AddDays(-7).Date)
               .GroupBy(p => p.Id)
               .Select(p => p.Key).ToList();

                mensageiro.Dados = entregasAtrasos.Count();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("cancelamentos7d")]
        public async Task<IActionResult> PercentualCancelamentos7D(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {   
                var entregas = _context.Pedidos.Include(p=> p.Status).Where(p => p.LojaId == loja && (p.Status.Id == 7 || p.Status.Id == 8 || p.Status.Id == 11) &&
                
                p.Data.Value.Date >= DateTime.Now.AddDays(-7).Date).ToList();

                if (entregas.Count > 0)
                {
                    try
                    {
                        var cancelados = entregas.Count(p => p.Status.Id == 8 || p.Status.Id == 11);
                        mensageiro.Dados = cancelados / (entregas.Count() * 1.0);
                    }
                    catch
                    {
                        mensageiro.Dados = 0;
                    }
                }
                else mensageiro.Dados = 0;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }


        [HttpPost("salvaravaliacao")]
        public async Task<Mensageiro> SalvarAvaliacao(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var pd = _context.Pedidos.FirstOrDefault(p => p.Id == pedido.Id);
                pd.ComentarioAvaliacao = pedido.ComentarioAvaliacao;
                _context.Entry(pd).Property(p => p.ComentarioAvaliacao).IsModified = true;
                _context.SaveChanges();
                //
                pedido.Avaliacoes.ForEach(o => {
                    o.PedidoId = o.Pedido.Id;
                    o.Pedido = null;
                });
                //
                _context.PedidoAvaliacoes.AddRange(pedido.Avaliacoes);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }
                
        [HttpGet("obterpedidosloja")]
        public async Task<Mensageiro> ObterPedidosLoja(uint loja, DateTime dtlocal)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                DateTime dtInicio = DateTime.UtcNow.AddHours(-dtlocal.TimeOfDay.TotalHours);
                DateTime dtFim = DateTime.UtcNow;
                //
                if (DateTime.UtcNow.Date == dtlocal.Date) {
                    dtInicio = DateTime.UtcNow.Date;
                }
                //
                _context.Database.BeginTransaction();
                //pega somente os que ainda não foram aceitos
                var pendentes = _context.Pedidos.Include(p=> p.Status)
                                                .Include(p=> p.Usuario)
                                                .Include(p=> p.FormaPagamento)
                                                .Where(p => p.Loja.Id == loja && p.Status.Id < 3)
                                                .OrderBy(p=> p.Id).ToList();                
                //pega somente os que estão em separação
                var separacao = _context.Pedidos.Include(p => p.Status)
                                                .Include(p => p.Usuario)
                                                .Include(p => p.FormaPagamento)
                                                .Where(p => p.Loja.Id == loja && p.Status.Id == 3)
                                                .OrderBy(p => p.Id).ToList();
                //pega somente os que saíram para entrega
                var entrega = _context.Pedidos.Include(p => p.Status)
                                              .Include(p => p.Usuario)
                                              .Include(p => p.FormaPagamento)
                                              .Where(p => p.Loja.Id == loja && p.Status.Id == 6)
                                              .OrderBy(p => p.Id).ToList();
                //pega somente os já foram entregue
                var entregue = _context.Pedidos.Include(p => p.Status)
                                               .Include(p => p.Usuario)
                                               .Include(p => p.FormaPagamento)
                                               .Where(p => p.Loja.Id == loja && p.Status.Id == 7 && dtInicio <= p.Data.Value && dtFim >= p.Data.Value)
                                               .OrderBy(p => p.Id).ToList();
                //pega somente os já foram entregue
                var cancelados = _context.Pedidos.Include(p => p.Status)
                                                 .Include(p => p.Usuario)
                                                 .Include(p => p.FormaPagamento)
                                                 .Where(p => p.Loja.Id == loja && p.Status.Id > 7 && dtInicio <= p.DataCancelamento.Value && dtFim >= p.DataCancelamento.Value)
                                                 .OrderByDescending(p => p.Id).ToList();
                //
                List<Pedido> pedidos = new List<Pedido>();
                pedidos.AddRange(pendentes);
                pedidos.AddRange(separacao);
                pedidos.AddRange(entrega);
                pedidos.AddRange(entregue);
                pedidos.AddRange(cancelados);
                pedidos.ForEach(o =>
                {
                    if (!string.IsNullOrEmpty(o.Ende))
                    {                        
                        if (o.Ende[o.Ende.Length - 1] != '}')
                            o.Ende = o.Ende + "}";
                        if (o.Ende.Contains("}}"))
                            o.Ende = o.Ende.Replace("}", "");
                        if (o.Ende[o.Ende.Length - 2] != '"' && !o.Ende.Contains("Longi"))
                            o.Ende = o.Ende.Replace(o.Ende[o.Ende.Length - 2] + o.Ende[o.Ende.Length - 1].ToString(), o.Ende[o.Ende.Length - 2] + "\"");
                        //
                        o.Endereco = Newtonsoft.Json.JsonConvert.DeserializeObject<Endereco>(o.Ende.ConvertUnicodeToText());                        
                    }
                });
                //
                mensageiro.Dados = pedidos;
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpGet("obtertotalpedidosloja")]
        public async Task<Mensageiro> ObterTotalPedidosloja(uint loja, DateTime dtlocal)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                DateTime dtInicio = DateTime.UtcNow.AddHours(-dtlocal.TimeOfDay.TotalHours);
                DateTime dtFim = DateTime.UtcNow;

                _context.Database.BeginTransaction();
                //pega somente os que ainda não foram aceitos
                mensageiro.Dados = _context.Pedidos.Include(p=> p.Loja).Where(p => p.Loja.Id == loja 
                                                            && p.Status.Id <= 6
                                                            || (p.Status.Id == 7 && dtInicio <= p.Data.Value && dtFim >= p.Data.Value))
                                                    .Sum(p=> p.Itens.Sum(q=> q.Preco * q.Quantidade) + p.Loja.TaxaEntrega);
                //
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpGet("obteritenspedido")]
        public async Task<Mensageiro> ObterItensPedidos(int pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                
               
                mensageiro.Dados = _context.PedidoItens.Include(p=> p.Produto).Where(p=> p.Pedido.Id == pedido);
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpPut("confirmar")]
        public async Task<Mensageiro> Confirmar(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var pd = _context.Pedidos.FirstOrDefault(p => p.Id == pedido.Id);
                pd.StatusPedidoId = pedido.Status.Id;
                pd.AtendenteId = pedido.Atendente.Id;
                _context.Entry(pd).Property(p => p.StatusPedidoId).IsModified = true;
                _context.Entry(pd).Property(p => p.AtendenteId).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpPut("entregar")]
        public async Task<Mensageiro> Entregar(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var pd = _context.Pedidos.FirstOrDefault(p => p.Id == pedido.Id);
                pd.StatusPedidoId = pedido.Status.Id;                
                _context.Entry(pd).Property(p => p.StatusPedidoId).IsModified = true;                
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpPut("concluir")]
        public async Task<Mensageiro> Concluir(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var pd = _context.Pedidos.FirstOrDefault(p => p.Id == pedido.Id);
                pd.StatusPedidoId = pedido.Status.Id;
                pd.DataEntrega = DateTime.UtcNow;
                _context.Entry(pd).Property(p => p.StatusPedidoId).IsModified = true;
                _context.Entry(pd).Property(p => p.DataEntrega).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpPut("cancelar")]
        public async Task<Mensageiro> Cancelar(Pedido pedido)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var pd = _context.Pedidos.FirstOrDefault(p => p.Id == pedido.Id);
                pd.StatusPedidoId = pedido.Status.Id;
                pd.AtendenteId = pedido.Atendente.Id;
                pd.DataCancelamento = DateTime.UtcNow;
                _context.Entry(pd).Property(p => p.StatusPedidoId).IsModified = true;
                _context.Entry(pd).Property(p => p.AtendenteId).IsModified = true;
                _context.Entry(pd).Property(p => p.DataCancelamento).IsModified = true;
                _context.Entry(pd).Property(p => p.JustificativaCancelamento).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;// "Falha na operação!";
                mensageiro.Dados = ex.StackTrace;
            }

            return mensageiro;
        }

        [HttpGet("obterpedidos")]
        public async Task<Mensageiro> ObterPedidos(int loja, DateTime dtini, DateTime dtfim, string filtro, int indice, int tamanho)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                if (string.IsNullOrEmpty(filtro))
                {
                    mensageiro.Dados = await Paginar<Pedido>.CreateAsync(_context.Pedidos
                                                                        .Where(p => p.Loja.Id == loja && p.Data.Value.Date >= dtini.Date && p.Data.Value.Date <= dtfim.Date)
                                                                        .Include(p => p.Atendente)
                                                                        .Include(p => p.FormaPagamento)
                                                                        .Include(p => p.Itens)
                                                                        .Include(p => p.Status)
                                                                        .OrderByDescending(p => p.Id), indice, tamanho);
                }
                else
                {
                    mensageiro.Dados = await Paginar<Pedido>.CreateAsync(_context.Pedidos
                                                                        .Where(p => p.Loja.Id == loja && p.Id.ToString().Contains(filtro)
                                                                        && p.Data.Value.Date >= dtini.Date && p.Data.Value.Date <= dtfim.Date)
                                                                        .Include(p => p.Atendente)
                                                                        .Include(p => p.FormaPagamento)
                                                                        .Include(p => p.Itens)
                                                                        .Include(p => p.Status)
                                                                        .OrderByDescending(p => p.Id), indice, tamanho);
                }
                           
            }
            catch (Exception ex) {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return mensageiro;
        }

        [HttpGet("obterprodutosvendidos")]
        public async Task<IActionResult> ObterProdutosVendidos(int loja, DateTime dtini, DateTime dtfim)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();
                //
                mensageiro.Dados = _context.PedidoItens
                                           .Include(p => p.Pedido)
                                           .Include(p => p.Produto)
                                           .ThenInclude(p => p.Categoria)
                                           .Where(p => p.Pedido.Loja.Id == loja && p.Pedido.Status.Id == 7 && p.Pedido.Data.Value.Date >= dtini.Date && p.Pedido.Data.Value.Date <= dtfim.Date)
                                        .ToList()
                                        .GroupBy(p => p.Produto)
                                        .Select(p => new PedidoItem { Produto = p.Key, Quantidade = (uint?)p.Sum(q => q.Quantidade), Preco = p.Sum(q => q.Quantidade * q.Preco) })
                                        .OrderByDescending(p=> p.Quantidade)
                                        .ToList();            
                
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

        [HttpGet("ultimopedido")]
        public async Task<IActionResult> ObterUltimoPedido(uint codUser)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                _context.Database.BeginTransaction();
                //
                mensageiro.Dados = _context.Pedidos
                                           .Include(p => p.Atendente)
                                           .Include(p => p.FormaPagamento)
                                           .Include(p => p.Itens)
                                           .Include(p => p.Status)
                                           .Where(p => p.Usuario.Id == codUser && p.Status.Id < 7)
                                           .OrderByDescending(p=> p.Data)
                                           .FirstOrDefault();
                                           
                                        

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
    }
}
