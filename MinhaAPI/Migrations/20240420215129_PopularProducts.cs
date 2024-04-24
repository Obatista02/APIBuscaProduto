﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaAPI.Migrations
{
    /// <inheritdoc />
    public partial class PopularProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder mb)
        {
          

            mb.Sql("Insert into produtos(Nome,Descricao,Preco,ImagemUrl,Estoque,DataCadastro,CategoriaId) Values ('Lanche de Atum','Lanche de Atum com maionese',8.50,'atum.jpg',10,now(),2)");

            mb.Sql("Insert into produtos(Nome,Descricao,Preco,ImagemUrl,Estoque,DataCadastro,CategoriaId) Values ('Pudim 100 g','Pudim de leite condensado 100g',6.75,'pudim.jpg',20,now(),3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder mb)
        {
            mb.Sql("Delete from Produtos");
        }
    }
}
