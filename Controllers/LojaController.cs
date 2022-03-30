using System;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllDelivery.Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using System.Text;

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/[controller]")]
    public class LojaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        readonly PasswordHasher _passwordHasher;

        public LojaController(ApplicationDbContext context, IOptions<HashingOptions> options)
        {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
        }

        [HttpGet("obterloja")]
        public async Task<IActionResult> ObterLoja(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.Lojas.FirstOrDefault(p => p.Id == loja);
                xx._Location = new Ponto(xx.Location.X, xx.Location.Y);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpPost("cadastrar")]
        public async Task<IActionResult> Cadastrar(Loja loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                if(loja.Location == null || loja.Location.IsEmpty) 
                {
                    loja.Location = new Point(-3.1103628672581847, - 60.04647621591336);
                }
                loja.HAbre = 800;
                loja.HFecha = 2100;
                loja.PedidoMinimo = 0;
                loja.TaxaEntrega = 0;
                loja.TempoMaximo = 60;
                loja.TempoMinimo = 30;
                loja.Raio = 5;
                _context.Database.BeginTransaction();
                _context.Lojas.Add(loja);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPost("cadastrarloja")]
        public async Task<IActionResult> CadastrarLoja(Loja loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.Lojas.Add(loja);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                    mensageiro.Mensagem = "Já existe uma loja com essa sequência!";
                else
                    mensageiro.Mensagem = "Falha ao cadastrar!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPost("cadastrarlojatarifa")]
        public async Task<IActionResult> CadastrarLojaTarifa(LojaTarifa tarifa)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.LojaTarifas.Add(tarifa);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                    mensageiro.Mensagem = "Já existe uma loja com essa sequência!";
                else
                    mensageiro.Mensagem = "Falha ao cadastrar!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.Lojas.FirstOrDefault(p => p.Id == loja);
                if (cc != null)
                    _context.Entry<Loja>(cc).State = EntityState.Detached;
                _context.Lojas.Remove(cc);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPut("atualizarloja")]
        public async Task<IActionResult> AtualizarLoja(Loja loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                //var loj = _context.Lojas.FirstOrDefault(p => p.Id == loja.Id);
                //if (loja.Location == null || loja.Location.IsEmpty)
                //{
                //    loja.Location = loj.Location;
                //}
                _context.Database.BeginTransaction();
                //var cc = _context.Lojas.FirstOrDefault(p => p.Id == loja.Id);
                //if (cc != null)
                //_context.Entry<Loja>(cc).State = EntityState.Detached;
                //_context.Lojas..Update(loja);
                //Loja
                _context.Entry<Loja>(loja).Property(p => p.ImgLogo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.ImgBanner).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.NomeFantasia).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.NomeRazao).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Descricao).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.TelefoneAlternativo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.TelefoneCelular).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.TelefoneComercial).IsModified = true;
                //Endereço
                _context.Entry<Loja>(loja).Property(p => p.CEP).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Bairro).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Cidade).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Endereco).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Numero).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Complemento).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.UF).IsModified = true;
                loja.Location = new Point(loja._Location.X, loja._Location.Y);
                _context.Entry<Loja>(loja).Property(p => p.Location).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Raio).IsModified = true;
                //Configurações
                _context.Entry<Loja>(loja).Property(p => p.TaxaEntrega).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.TempoMaximo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.TempoMinimo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Disponivel).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.IncluirComissao).IsModified = true;
                //
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPut("atualizar")]
        public async Task<IActionResult> Atualizar(Loja loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var loj = _context.Lojas.FirstOrDefault(p => p.Id == loja.Id);
                if (loja.CEP != loj.CEP || loja.Numero != loj.Numero) 
                {
                    loj.CEP = loja.CEP;
                    loj.Numero = loja.Numero;
                    loj.Endereco = loja.Endereco;
                    loj.Bairro = loja.Bairro;
                    loj.UF = loja.UF;
                    loj.Complemento = loja.Complemento;
                    loj.Cidade = loja.Cidade; 
                }
                if (loja.CNPJ != loj.CNPJ)
                    loj.CNPJ = loja.CNPJ;
                if (loja.NomeFantasia != loj.NomeFantasia)
                    loj.NomeFantasia = loja.NomeFantasia;
                if (loja.NomeRazao != loj.NomeRazao)
                    loj.NomeRazao = loja.NomeRazao;
                if (loj.Email != loja.Email)
                    loj.Email = loja.Email;
                if (loja.Descricao != loj.Descricao)
                    loj.Descricao = loja.Descricao;
                if (loja.Contato != loj.Contato)
                    loj.Contato = loja.Contato;
                if (loja.TelefoneAlternativo != loj.TelefoneAlternativo)
                    loj.TelefoneAlternativo = loja.TelefoneAlternativo;
                if (loja.TelefoneCelular != loj.TelefoneCelular)
                    loj.TelefoneCelular = loja.TelefoneCelular;
                if (loja.TelefoneComercial != loj.TelefoneComercial)
                    loj.TelefoneComercial = loja.TelefoneComercial;
                if(loja.ImgBanner != loj.ImgBanner || loja.ImgLogo != loj.ImgLogo) 
                {
                    loj.ImgBanner = loja.ImgBanner;
                    loj.ImgLogo = loja.ImgLogo;
                }

                _context.Database.BeginTransaction();
                var cc = _context.Lojas.FirstOrDefault(p => p.Id == loja.Id);
                if (cc != null)
                    _context.Entry<Loja>(cc).State = EntityState.Detached;
                _context.Lojas.Update(loj);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpPut("inativar")]
        public async Task<IActionResult> Inativar(Loja loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", loja.Ativo ? "Ativada" : "Inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(loja);
                _context.Entry<Loja>(loja).Property(p => p.Ativo).IsModified = true;
                _context.Entry<Loja>(loja).Property(p => p.Disponivel).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("paging")]
        public async Task<IActionResult> Paging(int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<Loja>.CreateAsync(_context.Lojas.OrderBy(p => p.Id).Select(p => new Loja
               {
                   Id = p.Id,
                   CNPJ = p.CNPJ,
                   NomeRazao = p.NomeRazao,
                   NomeFantasia = p.NomeFantasia,
                   Disponivel = p.Disponivel,
                   Ativo = p.Ativo,
                   Email = p.Email
               }), indice, total);
                
            }
            catch (Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpPost("enviaremail")]
        public async Task<IActionResult> EnviarEmail(Loja loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var us = _context.Usuarios.FirstOrDefault(p => p.Email == loja.Email && p.Loja.Id == loja.Id);

                if (us == null)
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = "Usuário não foi encontrado";
                    mensageiro.Dados = false;
                    return Ok(mensageiro);
                }

                string novasenha = GeneratePassword(8);
                us.Senha = _passwordHasher.Hash(novasenha);
                us.SenhaProv = true;
                _context.Entry<Usuario>(us).Property(p => p.Senha).IsModified = true;
                _context.Entry<Usuario>(us).Property(p => p.SenhaProv).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
                SmtpClient client = new SmtpClient();
                //
                // Para desenvolvimento
                client.Host = "smtp.zoho.com";
                client.Port = 587;
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential("suporte@elfarma.com.br", "123456s!S");
                //
                #region Corpo Email
                StringBuilder str = new StringBuilder();

                str.AppendLine("<html>");
                str.AppendLine("	<head>");
                str.AppendLine("		<style type=\"text/css\">");
                str.AppendLine("		.tg  {border-collapse:collapse;border-spacing:0;}");
                str.AppendLine("		.tg td{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;");
                str.AppendLine("		  overflow:hidden;padding:10px 5px;word-break:normal;}");
                str.AppendLine("		.tg th{border-color:black;border-style:solid;border-width:1px;font-family:Arial, sans-serif;font-size:14px;");
                str.AppendLine("		  font-weight:normal;overflow:hidden;padding:10px 5px;word-break:normal;}");
                str.AppendLine("		.tg .tg-zv4m{border-color:#ffffff;text-align:left;vertical-align:top}");
                str.AppendLine("		.tg .tg-2y37{border-color:#ffffff;font-size:24px;text-align:center;vertical-align:top}");
                str.AppendLine("		.tg .tg-fo2l{background-color:#3166ff;border-color:#3166ff;font-size:14px;text-align:left;vertical-align:top}");
                str.AppendLine("		.tg .tg-fbuf{background-color:#3166ff;border-color:#3166ff;text-align:left;vertical-align:top}");
                str.AppendLine("		.tg .tg-b420{border-color:#ffffff;font-size:18px;text-align:center;vertical-align:top}");
                str.AppendLine("		</style>	");
                str.AppendLine("	</head>");
                str.AppendLine("	<body>");
                str.AppendLine("		<table class=\"tg\">");
                str.AppendLine("		<thead>");
                str.AppendLine("		  <tr>");
                str.AppendLine("			<th class=\"tg-fo2l\"><span style=\"font-weight:bold; color:#FFF\">ElFarma</span></th>");
                str.AppendLine("			<th class=\"tg-fbuf\"></th>");
                str.AppendLine("			<th class=\"tg-fbuf\"></th>");
                str.AppendLine("		  </tr>");
                str.AppendLine("		</thead>");
                str.AppendLine("		<tbody>");
                str.AppendLine("		  <tr>");
                str.AppendLine("			<td class=\"tg-zv4m\" colspan=\"3\">Seja bem vindo ao ElFarma!<br/>Foi gerada uma senha provisória para seu acesso a plataforma. No seu primeiro acesso será solicitado a troca da senha.</td>");
                str.AppendLine("		  </tr>");
                str.AppendLine("		  <tr>");
                str.AppendLine("			<td class=\"tg-b420\" colspan=\"3\"><span style=\"color:#3166FF\">senha:</span></td>");
                str.AppendLine("		  </tr>");
                str.AppendLine("		  <tr>");
                str.AppendLine("			<td class=\"tg-2y37\" colspan=\"3\"><span style=\"font-weight:bold; color:#3166FF\">" + novasenha + "</span></td>");
                str.AppendLine("		  </tr>");
                str.AppendLine("		</tbody>");
                str.AppendLine("		</table>");
                str.AppendLine("	</body>");
                str.AppendLine("</html>");
                #endregion
                //
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("suporte@elfarma.com.br");
                mailMessage.To.Add(loja.Email);
                mailMessage.Body = str.ToString();
                mailMessage.Subject = "ElFarma - Senha Provisória";
                mailMessage.IsBodyHtml = true;
                client.Send(mailMessage);


                mensageiro.Dados = true;
            }
            catch(Exception ex)
            {
                _context.Database.RollbackTransaction();
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                mensageiro.Dados = false;
            }

            return Ok(mensageiro);
        }

        [HttpGet("ativas")]
        public IEnumerable<Loja> Ativas(double lat, double lon, TipoOrdenacao tipoOrdenacao)
        {
            List<Loja> valores = null;
            //
            switch (tipoOrdenacao)
            {
                case TipoOrdenacao.Distancia:
                    valores = _context.Lojas.OrderBy(p => p.NomeFantasia).ToList();
                    break;
                case TipoOrdenacao.TempoEntrega:
                    valores = _context.Lojas.OrderBy(p => p.TempoMinimo).ToList();
                    break;
                case TipoOrdenacao.TaxaEntrega:
                    valores = _context.Lojas.OrderBy(p => p.TaxaEntrega).ToList();
                    break;
                case TipoOrdenacao.OrdemAZ:
                default:
                    valores = _context.Lojas.OrderBy(p => p.NomeFantasia).ToList();
                    break;
            }
            //
            if (valores != null)
                valores.ForEach(o =>
                {
                    o.Referencia = new Point(lat, lon) { SRID = 2855 };
                });
            return valores;
        }

        [HttpGet("paginarloja")]
        public async Task<IActionResult> Paginarloja(uint loja, int indice, int total)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            try
            {
                var xx = await Paginar<Pedido>.CreateAsync(_context.Pedidos.Include(p => p.Itens).Include(p => p.Atendente).Include(p => p.FormaPagamento)
                        .Where(p => p.Loja.Id == loja && p.Status.Id == 3 && p.Itens.Count() > 0).AsNoTracking()
                        .OrderBy(p => p.Id), indice, total);
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);

        }

        [HttpGet("paginar")]
        public async Task<List<Loja>> Paginar(int segmento, int indice, int tamanho,double lat, double lon, TipoOrdenacao tipoOrdenacao)
        {
            Paginar<Loja> valores = null;
            //
            switch (tipoOrdenacao)
            {             
                case TipoOrdenacao.Distancia:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.Where(p=> p.Segmento == segmento).OrderBy(p => p.Location.Distance(new Point(lon, lat))).Select(p=> new Loja { 
                        Id = p.Id,
                       // ImgLogo = p.ImgLogo,
                      //  ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat))/1000),
                        PedidoMinimo = p.PedidoMinimo
                    } ), indice, tamanho);
                    break;
                case TipoOrdenacao.TempoEntrega:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.Where(p => p.Segmento == segmento).OrderBy(p => p.TempoMinimo).Select(p => new Loja
                    {
                        Id = p.Id,
                       // ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
                case TipoOrdenacao.TaxaEntrega:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.Where(p => p.Segmento == segmento).OrderBy(p => p.TaxaEntrega).Select(p => new Loja
                    {
                        Id = p.Id,
                        //ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
                case TipoOrdenacao.OrdemAZ:
                default:
                    valores = await Paginar<Loja>.CreateAsync(_context.Lojas.Where(p => p.Segmento == segmento).OrderBy(p => p.NomeFantasia).Select(p => new Loja
                    {
                        Id = p.Id,
                        //ImgLogo = p.ImgLogo,
                       // ImgBanner = p.ImgBanner,
                        NomeRazao = p.NomeRazao,
                        NomeFantasia = p.NomeFantasia,
                        HAbre = p.HAbre,
                        HFecha = p.HFecha,
                        TaxaEntrega = p.TaxaEntrega,
                        TempoMaximo = p.TempoMaximo,
                        TempoMinimo = p.TempoMinimo,
                        Disponivel = p.Disponivel,
                        Ativo = p.Ativo,
                        Distancia = string.Format("{0:n2} km", p.Location.Distance(new Point(lon, lat)) / 1000),
                        PedidoMinimo = p.PedidoMinimo
                    }), indice, tamanho);
                    break;
            }
           
            return valores.Itens;
        }

        [HttpGet("logo")]
        public IActionResult Logo(int loja)
        {
            var lj = _context.Lojas.FirstOrDefault(p => p.Id == loja);
            try
            {
                return Ok(new { Id = lj.Id, Logo = Convert.ToBase64String(lj.ImgLogo) });
            }
            catch {
                return Ok(new { Id = lj.Id });
            }
        }

        [HttpGet("banner")]
        public IActionResult Banner(int loja)
        {
            var lj = _context.Lojas.FirstOrDefault(p => p.Id == loja);
            try
            {
                return Ok(new { Id = lj.Id, Banner = Convert.ToBase64String(lj.ImgBanner) });
            }
            catch
            {
                return Ok(new { Id = lj.Id});
            }
        }

        [HttpGet("formaspagamento")]
        public IActionResult FormasPagamento(int loja)
        {
            var formas = _context.LojaFormaPagamentos.Include(p=> p.Loja)
                                                 .Include(p=> p.FormaPagamento)
                                                 .Where(p => p.Loja.Id == loja)
                                                 .Select(p=> p.FormaPagamento);
            return Ok(formas);
        }

        [HttpPut("abrirfechar")]
        public async Task<IActionResult> AbrirFechar(Loja loja)
        {
            Mensageiro mensageiro = new Mensageiro();
            mensageiro.Codigo = 200;
            mensageiro.Mensagem = "Operação realizada com sucesso!";
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(loja);                
                _context.Entry<Loja>(loja).Property(p => p.Disponivel).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("obtertarifas")]
        public async Task<IActionResult> ObterTarifas(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro();
            mensageiro.Codigo = 200;
            mensageiro.Mensagem = "Operação realizada com sucesso!";
            try
            {
                _context.Database.BeginTransaction();
                mensageiro.Dados = _context.LojaTarifas.FirstOrDefault(p => p.Loja.Id == loja && p.DtInicio.Value.Date <= DateTime.Now.Date && p.DtFim.Value.Date >= DateTime.Now.Date);
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("compromissofinanceiro")]
        public async Task<IActionResult> CompromissoFinanceiro(uint loja, int indice, int tamanho)
        {
            Mensageiro mensageiro = new Mensageiro();
            mensageiro.Codigo = 200;
            mensageiro.Mensagem = "Operação realizada com sucesso!";
            try
            {
                _context.Database.BeginTransaction();
                mensageiro.Dados = await Paginar<CompromissoFinanceiro>.CreateAsync(_context.CompromissoFinanceiros
                                           .Where(p => p.Loja.Id == loja).Select(p=> new CompromissoFinanceiro { 
                                           Id = p.Id,
                                           DataEmissao = p.DataEmissao,
                                           DataPagamento = p.DataPagamento,
                                           DataReferencia = p.DataReferencia,
                                           DataVencimento = p.DataVencimento,
                                           Descricao = p.Descricao,
                                           Valor = p.Valor
                                           }).OrderByDescending(p=> p.DataReferencia)
                                           , indice, tamanho);
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download(uint id)
        {
            Mensageiro mensageiro = new Mensageiro();
            mensageiro.Codigo = 200;
            mensageiro.Mensagem = "Operação realizada com sucesso!";
            try
            {
                _context.Database.BeginTransaction();
                mensageiro.Dados = _context.CompromissoFinanceiros.FirstOrDefault(p => p.Id == id);
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpGet("obtercontrato")]
        public async Task<IActionResult> ObterContrato(uint id)
        {
            Mensageiro mensageiro = new Mensageiro();
            mensageiro.Codigo = 200;
            mensageiro.Mensagem = "Operação realizada com sucesso!";
            try
            {
                _context.Database.BeginTransaction();
                mensageiro.Dados = _context.LojaTarifas.FirstOrDefault(p => p.LojaId == id && p.DtInicio < DateTime.Now.Date && p.DtFim > DateTime.Now.Date);
                _context.Database.CommitTransaction();
            }
            catch
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = "Falha ao realizar a operação!";
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        public string GeneratePassword(int Size)
        {
            string randomno = "abcdefghijklmnopqrstuvwyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < Size; i++)
            {
                ch = randomno[random.Next(0, randomno.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }

    }
}
