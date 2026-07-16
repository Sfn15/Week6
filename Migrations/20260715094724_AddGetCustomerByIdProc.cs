using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Week6.Migrations
{
    /// <inheritdoc />
    public partial class AddGetCustomerByIdProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_GetCustomerById
                @CustomerId INT
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT CustomerId, Email, FirstName, LastName, CreatedAt
                FROM Customers
                WHERE CustomerId = @CustomerId AND IsDeleted = 0;
            END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
