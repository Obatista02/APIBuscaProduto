﻿using MinhaAPI.Context;
using MinhaAPI.Repository.interfaces;

namespace MinhaAPI.Repository; 

public class UnitOfWork : IUnitOfWork
{
    private IProdutosRepository _produtoRepo;
    private ICategoriaRepository _categoriaRepo;
    public AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IProdutosRepository ProdutosRepository
    {
        get
        {
            return _produtoRepo = _produtoRepo ?? new ProdutoRepository(_context);
        }
    }
    public ICategoriaRepository CategoriaRepository
    {
        get
        {
            return _categoriaRepo = _categoriaRepo ?? new CategoriaRepository(_context);
        }
    }

    public IProdutosRepository ProdutoRepository => throw new NotImplementedException();

    public void Commit()
    {
        _context.SaveChanges();
    }
    public void Dispose()
    {
        _context.Dispose();
    }
}
