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
    public class StatusPedidoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        readonly PasswordHasher _passwordHasher;

        public StatusPedidoController(ApplicationDbContext context, IOptions<HashingOptions> options)
        {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
        }

        [HttpGet("obterstatus")]
        public async Task<IActionResult> ObterStatus(uint sp) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.StatusPedidos.FirstOrDefault(p => p.Id == sp);
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
        public async Task<IActionResult> Cadastrar(StatusPedido sp) 
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                var seq = _context.StatusPedidos.FirstOrDefault(p => p.Sequencia == sp.Sequencia);
                if (seq == null)
                {
                    _context.Database.BeginTransaction();
                    _context.StatusPedidos.Add(sp);
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                }
                else 
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = "Sequencia já está sendo utilizada em outro status!";
                }
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint sp)
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.StatusPedidos.FirstOrDefault(p => p.Id == sp);
                if (cc != null)
                    _context.Entry<StatusPedido>(cc).State = EntityState.Detached;
                _context.StatusPedidos.Remove(cc);
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
        public async Task<IActionResult> Atualizar(StatusPedido sp) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.StatusPedidos.FirstOrDefault(p => p.Id == sp.Id);
                if (cc != null)
                    _context.Entry<StatusPedido>(cc).State = EntityState.Detached;
                _context.StatusPedidos.Update(sp);
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
        public async Task<IActionResult> Inativar(StatusPedido sp)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", sp.Ativo ? "Ativada" : "Inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(sp);
                _context.Entry<StatusPedido>(sp).Property(p => p.Ativo).IsModified = true;
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
        public async Task<IActionResult> Paging(int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<StatusPedido>.CreateAsync(_context.StatusPedidos.OrderBy(p => p.Sequencia).Select(p => new StatusPedido
                {
                    Sequencia = p.Sequencia,
                    Nome = p.Nome,
                    Ativo = p.Ativo,
                    Descricao = p.Descricao,
                    Id = p.Id
                }), indice, total);
                
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
