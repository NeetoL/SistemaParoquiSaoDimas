using Microsoft.AspNetCore.Mvc;
using SaoDimas.CrossCutting;
using SaoDimas.Aplicacao;
using SaoDimas.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

// Registros ausentes e lifetimes incompatíveis falham na inicialização, em qualquer ambiente.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// Todo POST de formulário exige o token antiforgery.
builder.Services.AddControllersWithViews(options => { options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); options.Filters.Add<SaoDimas.CrossCutting.Filters.PermissoesParoquiaisFilter>(); options.Filters.Add<SaoDimas.CrossCutting.Filters.AuditoriaCadastrosFilter>(); });
builder.Services.AddProblemDetails();

builder.Services
    .AddAplicacao()
    .AddInfraestrutura(builder.Configuration)
    .AddCrossCutting(builder.Configuration)
    .AddAutenticacaoWeb(builder.Environment.IsDevelopment());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePages(contexto => SaoDimas.MVC.UI.PaginasEstado.RenderizarAsync(contexto.HttpContext));
app.UseRequestLocalization("pt-BR");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
