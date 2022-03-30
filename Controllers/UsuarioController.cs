using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AllDelivery.Lib;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace AllDelivery.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private ApplicationDbContext _context;
        private readonly PasswordHasher _passwordHasher;
        private readonly SigningConfigurations _signingConfigurations;
        private readonly TokenConfiguration _tokenConfigurations;

        public UsuarioController(ApplicationDbContext context, IOptions<HashingOptions> options, SigningConfigurations signingConfigurations, TokenConfiguration tokenConfiguration) {
            _context = context;
            _passwordHasher = new PasswordHasher(options);
            _signingConfigurations = signingConfigurations;
            _tokenConfigurations = tokenConfiguration;
        }
        
        [HttpGet("obter")]
        public async Task<IActionResult> Obter(Login login) 
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                var us = _context.Usuarios.ToList();
                mensageiro.Dados = us;
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
            }
            return Ok(mensageiro);
        }

        [AllowAnonymous]
        [HttpPost("logar")]
        public async Task<IActionResult> Logar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();              

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email);
                //
                if (_usuario != null)
                {
                    _usuario.Anonimo = false;

                    if (string.IsNullOrEmpty(_usuario.Email))
                        _usuario.Email = login.Email;

                    if (_usuario.TokenFCM != login.TokenFCM)
                    {
                        _usuario.TokenFCM = login.TokenFCM;
                    }

                    if (_passwordHasher.Check(_usuario.Senha, login.Senha))
                    {
                        ClaimsIdentity identity = new ClaimsIdentity(
                                                   new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                                   new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                                   }
                                               );
                        //
                        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                        HttpContext.User = claimsPrincipal;
                        //
                        if (!string.IsNullOrEmpty(login.Nome) && (_usuario.Nome == "Guest" || string.IsNullOrEmpty(_usuario.Nome)))
                        {
                            _usuario.Nome = login.Nome;

                        }

                        if (!string.IsNullOrEmpty(login.Sobrenome) && (_usuario.Sobrenome == " " || string.IsNullOrEmpty(_usuario.Sobrenome)))
                        {

                            _usuario.Sobrenome = login.Sobrenome;
                        }
                        //
                        _usuario.TokenCreate = DateTime.Now;
                        _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                        var handler = new JwtSecurityTokenHandler();
                        var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                        {
                            Issuer = _tokenConfigurations.Issuer,
                            Audience = _tokenConfigurations.Audience,
                            SigningCredentials = _signingConfigurations.SigningCredentials,
                            Subject = identity,
                            NotBefore = _usuario.TokenCreate,
                            Expires = _usuario.TokenExpiration
                        });
                        //Cria o token de acesso
                        _usuario.Token = handler.WriteToken(securityToken);
                        _usuario.DataUltimoLogin = DateTime.Now;
                        //salva o token de acesso
                        _context.Attach(_usuario);
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenFCM).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Anonimo).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Nome).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Sobrenome).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Email).IsModified = true;
                        //

                        _context.SaveChanges();
                        //
                        mensageiro.Dados = _usuario;
                    }
                    else
                    {
                        mensageiro.Codigo = 300;
                        mensageiro.Mensagem = "Usuário ou senha inválido!";
                    }
                }
                else
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = "Usuário ou senha inválido!";
                }
                _context.Database.CommitTransaction();
                /*}
                else
                    mensageiro.Mensagem = "Usuário ou senha inválido!";   */
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [AllowAnonymous]
        [HttpPost("logarloja")]
        public async Task<IActionResult> LogarLoja(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.Include(p=> p.Loja).FirstOrDefault(p => p.Email == login.Email && p.Loja != null && !string.IsNullOrEmpty(p.Senha));
                //
                if (_usuario != null)
                {
                    _usuario.Anonimo = false;

                    if (string.IsNullOrEmpty(_usuario.Email))
                        _usuario.Email = login.Email;

                    if (_usuario.TokenFCM != login.TokenFCM)
                    {
                        _usuario.TokenFCM = login.TokenFCM;
                    }

                    if (_passwordHasher.Check(_usuario.Senha, login.Senha))
                    {                       
                        ClaimsIdentity identity = new ClaimsIdentity(
                                                   new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                                   new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                                   }
                                               );
                        //
                        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                        HttpContext.User = claimsPrincipal;
                        //
                        if (!string.IsNullOrEmpty(login.Nome) && (_usuario.Nome == "Guest" || string.IsNullOrEmpty(_usuario.Nome)))
                        {
                            _usuario.Nome = login.Nome;

                        }

                        if (!string.IsNullOrEmpty(login.Sobrenome) && (_usuario.Sobrenome == " " || string.IsNullOrEmpty(_usuario.Sobrenome)))
                        {

                            _usuario.Sobrenome = login.Sobrenome;
                        }
                        //
                        _usuario.TokenCreate = DateTime.Now;
                        _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                        var handler = new JwtSecurityTokenHandler();
                        var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                        {
                            Issuer = _tokenConfigurations.Issuer,
                            Audience = _tokenConfigurations.Audience,
                            SigningCredentials = _signingConfigurations.SigningCredentials,
                            Subject = identity,
                            NotBefore = _usuario.TokenCreate,
                            Expires = _usuario.TokenExpiration
                        });
                        //Cria o token de acesso
                        _usuario.Token = handler.WriteToken(securityToken);
                        _usuario.DataUltimoLogin = DateTime.Now;
                        //salva o token de acesso
                        _context.Attach(_usuario);
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.TokenFCM).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Anonimo).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Nome).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Sobrenome).IsModified = true;
                        _context.Entry<Usuario>(_usuario).Property(c => c.Email).IsModified = true;
                        //
                        _context.SaveChanges();
                        //
                        _usuario.Loja.Tarifas = _context.LojaTarifas.Where(p => p.LojaId == _usuario.Loja.Id && p.DtInicio < DateTime.Now.Date && p.DtFim > DateTime.Now.Date).ToList();
                        _usuario.Loja.Tarifas.ForEach(o => { o.Loja = null; });
                        //
                        mensageiro.Dados = _usuario;
                    }
                    else
                    {
                        mensageiro.Codigo = 300;
                        mensageiro.Mensagem = "Usuário ou senha inválido!";
                    }
                }
                else
                {
                    mensageiro.Codigo = 300;
                    mensageiro.Mensagem = "Usuário ou senha inválido!";
                }
                _context.Database.CommitTransaction();
                /*}
                else
                    mensageiro.Mensagem = "Usuário ou senha inválido!";   */
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [HttpPost("enviaremail")]
        public async Task<IActionResult> Enviar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email);

                if (_usuario != null)
                {
                    var senhaProvisoria = GeneratePassword(8);
                    _usuario.Senha = _passwordHasher.Hash(senhaProvisoria);
                    _usuario.SenhaProv = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    //
                    EnviarEmail(login.Email, senhaProvisoria);
                }
                else
                    _context.Database.RollbackTransaction();
            }
            catch(Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        private string GeneratePassword(int Size)
        {
            string randomno = "abcdefghijklmnopqrstuvwyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < Size; i++)
            {
                ch = randomno[random.Next(0, randomno.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private bool EnviarEmail(string email, string novasenha)
        {
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
            str.AppendLine("			<th class=\"tg-fo2l\"><span style=\"font-weight:bold; color:#FFF\">Appmed</span></th>");
            str.AppendLine("			<th class=\"tg-fbuf\"></th>");
            str.AppendLine("			<th class=\"tg-fbuf\"></th>");
            str.AppendLine("		  </tr>");
            str.AppendLine("		</thead>");
            str.AppendLine("		<tbody>");
            str.AppendLine("		  <tr>");
            str.AppendLine("			<td class=\"tg-zv4m\" colspan=\"3\">Foi gerada uma senha provisória para seu acesso a plataforma. No seu primeiro acesso será solicitado a troca de senha</td>");
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
            str.AppendLine("</htmla>");
            #endregion
            //
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("suporte@elfarma.com.br");
            mailMessage.To.Add(email);
            mailMessage.Body = str.ToString();
            mailMessage.Subject = "ElFarma - Senha Provisória";
            mailMessage.IsBodyHtml = true;
            client.Send(mailMessage);

            return true;
        }

        [HttpPut("redefinir")]
        public async Task<IActionResult> Redefinir(Usuario usuario)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso");
            mensageiro.Dados = false;

            try
            {
                _context.Database.BeginTransaction();               
                //
                usuario.Senha = _passwordHasher.Hash(usuario.Senha);
                usuario.SenhaProv = false;
                _context.Attach(usuario);
                _context.Entry<Usuario>(usuario).Property(p => p.Senha).IsModified = true;
                _context.Entry<Usuario>(usuario).Property(p => p.SenhaProv).IsModified = true;
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                mensageiro.Codigo = 300;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        //public async Task<IActionResult> Cadastrar(Usuario us)
        //{
        //    string mensagem = "Loja cadastrada com sucesso!";
        //    try
        //    {
        //        _context.Database.BeginTransaction();
        //        _context.Usuarios.Add(us);
        //        _context.SaveChanges();
        //        _context.Database.CommitTransaction();
        //    }
        //    catch (Exception ex)
        //    {
        //        mensagem = "Falha ao cadastrar o item!";
        //        _context.Database.RollbackTransaction();
        //    }
        //    return mensagem;
        //}

        [AllowAnonymous]
        [HttpPost("autenticar")]
        public async Task<IActionResult> Autenticar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                await FirebaseMessaging.DefaultInstance.SendAsync(new Message { Token = login.TokenFCM }, true);

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email || p.TokenFCM == login.TokenFCM);
                //
                if (_usuario != null)
                {
                    _usuario.Anonimo = false;

                    if (string.IsNullOrEmpty(_usuario.Email))
                        _usuario.Email = login.Email;

                    if (_usuario.TokenFCM != login.TokenFCM)
                    {
                        _usuario.TokenFCM = login.TokenFCM;
                    }

                    /*  if (_passwordHasher.Check(_usuario.Senha, login.Senha))
                      {*/
                    ClaimsIdentity identity = new ClaimsIdentity(
                                               new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                               new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                               }
                                           );
                    //
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                    HttpContext.User = claimsPrincipal;
                    //
                    if (!string.IsNullOrEmpty(login.Nome) && (_usuario.Nome == "Guest" || string.IsNullOrEmpty(_usuario.Nome)))
                    {
                        _usuario.Nome = login.Nome;

                    }

                    if (!string.IsNullOrEmpty(login.Sobrenome) && (_usuario.Sobrenome == " " || string.IsNullOrEmpty(_usuario.Sobrenome)))
                    {

                        _usuario.Sobrenome = login.Sobrenome;
                    }
                    //
                    _usuario.TokenCreate = DateTime.Now;
                    _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                    var handler = new JwtSecurityTokenHandler();
                    var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                    {
                        Issuer = _tokenConfigurations.Issuer,
                        Audience = _tokenConfigurations.Audience,
                        SigningCredentials = _signingConfigurations.SigningCredentials,
                        Subject = identity,
                        NotBefore = _usuario.TokenCreate,
                        Expires = _usuario.TokenExpiration
                    });
                    //Cria o token de acesso
                    _usuario.Token = handler.WriteToken(securityToken);
                    _usuario.DataUltimoLogin = DateTime.Now;
                    //salva o token de acesso
                    _context.Attach(_usuario);
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.TokenFCM).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Anonimo).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Nome).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Sobrenome).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(c => c.Email).IsModified = true;
                    //

                    _context.SaveChanges();
                    //
                    mensageiro.Dados = _usuario;
                }
                else
                {
                    mensageiro.Mensagem = "Usuário ou senha inválido!";
                }
                _context.Database.CommitTransaction();
                /*}
                else
                    mensageiro.Mensagem = "Usuário ou senha inválido!";   */
            }
            catch (Exception ex)
            {
                mensageiro.Codigo = 300;
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [AllowAnonymous]
        [HttpPost("autenticartoken")]
        public async Task<IActionResult> AutenticarToken(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();
                await FirebaseMessaging.DefaultInstance.SendAsync(new Message { Token = login.TokenFCM }, true);
                //FirebaseToken fba = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(login.TokenFCM);
                //if (fba. == TaskStatus.Faulted)
                //    throw new Exception("Token inválido!");

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.TokenFCM == login.TokenFCM);

                if (_usuario == null)
                {
                    _usuario = new Usuario { TokenFCM = login.TokenFCM };
                    _usuario.CodeVerification = " ";
                    _usuario.Nome = "Guest";
                    _usuario.Sobrenome = " ";
                    _usuario.Anonimo = true;
                    _context.Usuarios.Add(_usuario);
                    _context.SaveChanges();
                }
                //
                ClaimsIdentity identity = new ClaimsIdentity(
                                               new GenericIdentity(_usuario.Id.ToString(), "Login"),
                                               new[] {
                                             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                                             new Claim(JwtRegisteredClaimNames.UniqueName, _usuario.Id.ToString())
                                               }
                                           );
                //
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                HttpContext.User = claimsPrincipal;
                //
                _usuario.TokenCreate = DateTime.Now;
                _usuario.TokenExpiration = _usuario.TokenCreate + TimeSpan.FromSeconds(_tokenConfigurations.Seconds);

                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = _tokenConfigurations.Issuer,
                    Audience = _tokenConfigurations.Audience,
                    SigningCredentials = _signingConfigurations.SigningCredentials,
                    Subject = identity,
                    NotBefore = _usuario.TokenCreate,
                    Expires = _usuario.TokenExpiration
                });
                //Cria o token de acesso
                _usuario.Token = handler.WriteToken(securityToken);
                _usuario.DataUltimoLogin = DateTime.Now;
                //salva o token de acesso
                _context.Attach(_usuario);
                _context.Entry<Usuario>(_usuario).Property(c => c.TokenCreate).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.TokenExpiration).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.Token).IsModified = true;
                _context.Entry<Usuario>(_usuario).Property(c => c.DataUltimoLogin).IsModified = true;
                _context.SaveChanges();
                //
                mensageiro.Dados = _usuario;
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

        [Authorize("Bearer")]
        [HttpPost("verificar")]
        public IActionResult Verificar(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Email == login.Email && p.CodeVerification == login.CodeVerification);

                if (_usuario != null)
                {
                    _usuario.Validated = true;
                    _context.Entry<Usuario>(_usuario).Property(p=> p.Validated).IsModified = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    _usuario.Senha = null;
                    mensageiro.Dados = _usuario;

                }
                else
                    mensageiro.Mensagem = "Código inválido";
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [Authorize("Bearer")]
        [HttpPost("novo")]
        public IActionResult Novo(Login login)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.TokenFCM == login.TokenFCM);

                if (_usuario != null)
                {
                    _usuario.Validated = true;
                    _usuario.Email = login.Email;
                    _usuario.Nome = " ";
                    _usuario.Anonimo = false;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Validated).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Email).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Nome).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Anonimo).IsModified = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    _usuario.Senha = null;
                    mensageiro.Dados = _usuario;

                }
                else
                    mensageiro.Mensagem = "Código inválido";
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }

        [Authorize("Bearer")]
        [HttpPost("atualizar")]
        public IActionResult Atualizar(Usuario usuario)
        {
            Mensageiro mensageiro = new Mensageiro(200, "Operação realizada com sucesso!");
            try
            {
                _context.Database.BeginTransaction();

                var _usuario = _context.Usuarios.FirstOrDefault(p => p.Id == usuario.Id);

                if (_usuario != null)
                {
                    _usuario.CPF = usuario.CPF;
                    _usuario.Telefone = usuario.Telefone;

                    _context.Entry<Usuario>(_usuario).Property(p => p.CPF).IsModified = true;
                    _context.Entry<Usuario>(_usuario).Property(p => p.Telefone).IsModified = true;
                    _context.SaveChanges();
                    _context.Database.CommitTransaction();
                    _usuario.Senha = null;
                    mensageiro.Dados = _usuario;

                }
                else
                    mensageiro.Mensagem = "Código inválido";
            }
            catch (Exception ex)
            {
                mensageiro.Mensagem = ex.Message;
                _context.Database.RollbackTransaction();
            }

            return Ok(mensageiro);
        }
    }
}
