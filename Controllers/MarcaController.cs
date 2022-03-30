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
    public class MarcaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        readonly PasswordHasher _passwordHasher;

        public MarcaController(ApplicationDbContext context, IOptions<HashingOptions> options)
        {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
        }

        [HttpGet("obtermarca")]
        public async Task<IActionResult> ObterMarca(uint marca) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                var xx = _context.Marcas.FirstOrDefault(p => p.Id == marca);
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
        public async Task<IActionResult> Cadastrar(Marca marca) 
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.Marcas.Add(marca);
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

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint marca)
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.Marcas.FirstOrDefault(p => p.Id == marca);
                if (cc != null)
                    _context.Entry<Marca>(cc).State = EntityState.Detached;
                _context.Marcas.Remove(cc);
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
        public async Task<IActionResult> Atualizar(Marca marca) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                var cc = _context.Marcas.FirstOrDefault(p => p.Id == marca.Id);
                if (cc != null)
                    _context.Entry<Marca>(cc).State = EntityState.Detached;
                _context.Marcas.Update(marca);
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
        public async Task<IActionResult> Inativar(Marca marca)
        {
            Mensageiro mensageiro = new Mensageiro(200, string.Format("{0} com sucesso!", marca.Ativo ? "Ativada" : "Inativada"));
            try
            {
                _context.Database.BeginTransaction();
                _context.Attach(marca);
                _context.Entry<Marca>(marca).Property(p => p.Ativo).IsModified = true;
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


        [HttpGet("paginar")]
        public async Task<IActionResult> PaginarLoja(uint loja, int indice, int total) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = await Paginar<Marca>.CreateAsync(_context.Marcas.Where(p => p.LojaId == loja).AsNoTracking()
                          .OrderBy(p => p.Id), indice, total);
            }

            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [HttpGet("obtermarcaativo")]
        public async Task<IActionResult> ObterMarcaAtivo(uint loja)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var xx = _context.Marcas.Where(p => p.LojaId == loja).Where(p => p.Ativo == true).ToList();
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
