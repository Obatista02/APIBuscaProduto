﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinhaAPI.Context;
using MinhaAPI.Models;

namespace MinhaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }
    [HttpGet("produtos")]
    public ActionResult<IEnumerable<Categoria>> GetCategoriaProdutos()
    {
        return _context.Categorias.Include(p=> p.Produtos).Where(c=>c.CategoriaId <= 5).AsNoTracking().ToList();
    }

    [HttpGet]
    public ActionResult<IEnumerable<Categoria>> Get()
    {

        return _context.Categorias.AsNoTracking().ToList();

    }
    [HttpGet("{id:int}", Name = "ObterCategoria")]
    public ActionResult<Categoria> get(int id)
    {
        //throw new Exception("Exceçao ao retonroar a categoria pelo id");

        var categoria = _context.Categorias.FirstOrDefault(c => c.CategoriaId == id);
        if(categoria is null)
        {
            return NotFound();
        }
        return categoria;
    }
    [HttpPost]
    public ActionResult Post(Categoria categoria)
    {
        if(categoria is null)
        {
            return BadRequest();
        }
       _context.Categorias.Add(categoria);
       _context.SaveChanges();

        return new CreatedAtRouteResult("ObterCategoria", new {id = categoria.CategoriaId}, categoria);
    }
    [HttpPut("{id:int}")]
    public ActionResult Put(int id, Categoria categoria)
    {
        if (id != categoria.CategoriaId)
        {
            return BadRequest();
        }
        _context.Entry(categoria).State = EntityState.Modified;
        _context.SaveChanges();
        return Ok(categoria);
    }
    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        var categoria = _context.Categorias.FirstOrDefault(c=> c.CategoriaId==id);
        if(categoria is null)
        {
            return NotFound("Categoria nao localizada");
        }
        _context.Categorias.Remove(categoria);
        _context.SaveChanges();
        return Ok(categoria);
    }
}