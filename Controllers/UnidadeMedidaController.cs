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

namespace AllDelivery.Api.Controllers
{
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/[controller]")]
    public class UnidadeMedidaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        readonly PasswordHasher _passwordHasher;

        public UnidadeMedidaController(ApplicationDbContext context, IOptions<HashingOptions> options)
        {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
        }

        [HttpGet("obter")]
        public async Task<IActionResult> Obter(uint um) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.UnidadeMedidas.FirstOrDefault(p => p.Id == um);
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
        public async Task<IActionResult> Cadastrar(UnidadeMedida um) 
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.UnidadeMedidas.Add(um);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = "Já existe uma Unidade de Medida com essa sequência!";
                }
                else
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = ex.Message;
                }
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint um)
        {
            Mensageiro mensageiro = new Mensageiro(200 ,"Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.UnidadeMedidas.FirstOrDefault(p => p.Id == um);
                if (cc != null)
                    _context.Entry<UnidadeMedida>(cc).State = EntityState.Detached;
                _context.UnidadeMedidas.Remove(cc);
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

        [HttpPut("atualizar")]
        public async Task<IActionResult> Atualizar(UnidadeMedida um) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.UnidadeMedidas.FirstOrDefault(p => p.Id == um.Id);
                if (cc != null)
                    _context.Entry<UnidadeMedida>(cc).State = EntityState.Detached;
                _context.UnidadeMedidas.Update(um);
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
        public async Task<IActionResult> Inativar(UnidadeMedida um)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", um.Ativo ? "Ativada" : "Inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(um);
                _context.Entry<UnidadeMedida>(um).Property(p => p.Ativo).IsModified = true;
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

        [HttpGet("paging")]
        public async Task<IActionResult> PagingGestao(int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<UnidadeMedida>.CreateAsync(_context.UnidadeMedidas.OrderBy(p => p.Id), indice, total);
                
            }
            catch (Exception ex) 
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("paginar")]
        public async Task<IActionResult> PaginarLoja(uint loja, int indice, int total)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<UnidadeMedida>.CreateAsync(_context.UnidadeMedidas.Where(p => p.LojaId == loja).AsNoTracking()
                          .OrderBy(p => p.Id), indice, total);
            }

            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obterunidademedidaativo")]
        public async Task<IActionResult> ObterUnidadeMedidaAtivo(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var xx = _context.UnidadeMedidas.Where(p => p.LojaId == loja).Where(p => p.Ativo == true).ToList();
                mensageiro.Dados = xx;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }
    }
}
