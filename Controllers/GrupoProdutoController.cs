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
    public class GrupoProdutoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;        

        public GrupoProdutoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("obtergrupoprodutos")]
        public async Task<IActionResult> ObterGrupoProdutos(uint loja) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try 
            {
                mensageiro.Dados = _context.GrupoProdutos.Include(p=> p.Grupo).Where(p => p.Grupo.Loja.Id == loja);                
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpGet("obtergrupoprodutos2")]
        public async Task<IActionResult> ObterGrupoProdutos2(uint grupo)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                mensageiro.Dados = _context.GrupoProdutos.Include(p => p.Grupo).Include(p=> p.Produto).Where(p => p.Grupo.Id == grupo);
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }

            return Ok(mensageiro);
        }

        [HttpPost("cadastrar")]
        public async Task<IActionResult> Cadastrar(GrupoProduto grupoproduto) 
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                _context.GrupoProdutos.Add(grupoproduto);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                if (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
                    mensageiro.Mensagem = "Já existe uma marca com essa sequência!";
               
                _context.Database.RollbackTransaction();
            }
            return Ok(mensageiro);
        }

        [HttpDelete("excluir")]
        public async Task<IActionResult> Excluir(uint grupo, uint produto)
        {
            Mensageiro mensageiro = new Mensageiro(200 , "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();                
                _context.GrupoProdutos.Remove(_context.GrupoProdutos.FirstOrDefault(p => p.Grupo.Id == grupo && p.Produto.Id == produto));
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
        public async Task<IActionResult> Atualizar(GrupoProduto grupoproduto) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();                
                _context.GrupoProdutos.Update(grupoproduto);
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
        

    }
}
