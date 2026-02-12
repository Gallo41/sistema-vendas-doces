-- ============================================
-- Script de Criação do Banco de Dados
-- Sistema de Pedidos de Doces
-- ============================================

-- Criar o banco de dados
CREATE DATABASE IF NOT EXISTS pedidos_doces
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE pedidos_doces;

-- ============================================
-- Tabela: Clientes
-- ============================================
CREATE TABLE Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    Telefone VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NULL,
    Endereco VARCHAR(300) NULL,
    Observacoes TEXT NULL,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_nome (Nome),
    INDEX idx_telefone (Telefone)
) ENGINE=InnoDB;

-- ============================================
-- Tabela: Produtos
-- ============================================
CREATE TABLE Produtos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Tipo VARCHAR(50) NOT NULL COMMENT 'Trufa ou Pão de Mel',
    Sabor VARCHAR(100) NOT NULL,
    PrecoUnitario DECIMAL(10, 2) NOT NULL,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_tipo (Tipo),
    INDEX idx_sabor (Sabor),
    UNIQUE KEY unique_tipo_sabor (Tipo, Sabor)
) ENGINE=InnoDB;

-- ============================================
-- Tabela: Pedidos
-- ============================================
CREATE TABLE Pedidos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ClienteId INT NOT NULL,
    DataPedido DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataEntrega DATETIME NULL,
    ValorTotal DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    Status VARCHAR(50) NOT NULL DEFAULT 'Pendente' COMMENT 'Pendente, Em Produção, Pronto, Entregue, Cancelado',
    Observacoes TEXT NULL,
    INDEX idx_cliente (ClienteId),
    INDEX idx_data_pedido (DataPedido),
    INDEX idx_data_entrega (DataEntrega),
    INDEX idx_status (Status),
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- ============================================
-- Tabela: ItensPedido
-- ============================================
CREATE TABLE ItensPedido (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    ProdutoId INT NOT NULL,
    Quantidade INT NOT NULL,
    PrecoUnitario DECIMAL(10, 2) NOT NULL,
    Subtotal DECIMAL(10, 2) NOT NULL,
    INDEX idx_pedido (PedidoId),
    INDEX idx_produto (ProdutoId),
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- ============================================
-- Tabela: Pagamentos
-- ============================================
CREATE TABLE Pagamentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    DataPagamento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ValorPago DECIMAL(10, 2) NOT NULL,
    FormaPagamento VARCHAR(50) NOT NULL COMMENT 'Dinheiro, PIX, Cartão Débito, Cartão Crédito',
    Observacoes TEXT NULL,
    INDEX idx_pedido (PedidoId),
    INDEX idx_data_pagamento (DataPagamento),
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================
-- Inserir Cliente Padrão
-- ============================================
INSERT INTO Clientes (Nome, Telefone, Email, Ativo) 
VALUES ('Cliente Padrão', '00000000000', NULL, TRUE);

-- ============================================
-- Inserir Produtos de Exemplo (Opcional)
-- ============================================
-- Trufas
INSERT INTO Produtos (Tipo, Sabor, PrecoUnitario) VALUES
('Trufa', 'Chocolate', 4,00),
('Trufa', 'Morango', 4,00),
('Trufa', 'Maracujá', 4,00),
('Trufa', 'Limão', 4,00),
('Trufa', 'Beijinho', 4,00),


-- Pães de Mel
INSERT INTO Produtos (Tipo, Sabor, PrecoUnitario) VALUES
('Pão de Mel', 'Tradicional', 8.00),
('Pão de Mel', 'Doce de Leite', 8.00),
('Pão de Mel', 'Prestígio', 8.00);

-- ============================================
-- Views Úteis para Relatórios
-- ============================================

-- View: Pedidos com Saldo Devedor
CREATE OR REPLACE VIEW vw_PedidosComSaldo AS
SELECT 
    p.Id AS PedidoId,
    p.DataPedido,
    p.DataEntrega,
    p.Status,
    c.Nome AS Cliente,
    c.Telefone,
    p.ValorTotal,
    COALESCE(SUM(pg.ValorPago), 0) AS TotalPago,
    (p.ValorTotal - COALESCE(SUM(pg.ValorPago), 0)) AS SaldoDevedor
FROM Pedidos p
INNER JOIN Clientes c ON p.ClienteId = c.Id
LEFT JOIN Pagamentos pg ON p.Id = pg.PedidoId
GROUP BY p.Id, p.DataPedido, p.DataEntrega, p.Status, c.Nome, c.Telefone, p.ValorTotal
HAVING SaldoDevedor > 0
ORDER BY p.DataPedido DESC;

-- View: Produtos Mais Vendidos
CREATE OR REPLACE VIEW vw_ProdutosMaisVendidos AS
SELECT 
    pr.Tipo,
    pr.Sabor,
    COUNT(ip.Id) AS QuantidadePedidos,
    SUM(ip.Quantidade) AS QuantidadeTotal,
    SUM(ip.Subtotal) AS ValorTotalVendido
FROM Produtos pr
INNER JOIN ItensPedido ip ON pr.Id = ip.ProdutoId
INNER JOIN Pedidos p ON ip.PedidoId = p.Id
WHERE p.Status != 'Cancelado'
GROUP BY pr.Id, pr.Tipo, pr.Sabor
ORDER BY QuantidadeTotal DESC;

-- View: Clientes que Mais Compram
CREATE OR REPLACE VIEW vw_ClientesTop AS
SELECT 
    c.Id AS ClienteId,
    c.Nome,
    c.Telefone,
    COUNT(p.Id) AS TotalPedidos,
    SUM(p.ValorTotal) AS ValorTotalComprado,
    COALESCE(SUM(pg.ValorPago), 0) AS TotalPago,
    (SUM(p.ValorTotal) - COALESCE(SUM(pg.ValorPago), 0)) AS SaldoDevedor
FROM Clientes c
INNER JOIN Pedidos p ON c.Id = p.ClienteId
LEFT JOIN Pagamentos pg ON p.Id = pg.PedidoId
WHERE p.Status != 'Cancelado'
GROUP BY c.Id, c.Nome, c.Telefone
ORDER BY ValorTotalComprado DESC;

-- View: Relatório de Produção (Sabores para Produzir)
CREATE OR REPLACE VIEW vw_RelatorioProducao AS
SELECT 
    pr.Tipo,
    pr.Sabor,
    SUM(ip.Quantidade) AS QuantidadeTotal,
    COUNT(DISTINCT p.Id) AS NumeroPedidos,
    GROUP_CONCAT(DISTINCT c.Nome ORDER BY c.Nome SEPARATOR ', ') AS Clientes
FROM Produtos pr
INNER JOIN ItensPedido ip ON pr.Id = ip.ProdutoId
INNER JOIN Pedidos p ON ip.PedidoId = p.Id
INNER JOIN Clientes c ON p.ClienteId = c.Id
WHERE p.Status IN ('Pendente', 'Em Produção')
GROUP BY pr.Tipo, pr.Sabor
ORDER BY pr.Tipo, QuantidadeTotal DESC;

-- ============================================
-- Stored Procedures Úteis
-- ============================================

-- Procedure: Calcular Total do Pedido
DELIMITER //
CREATE PROCEDURE sp_CalcularTotalPedido(IN pedido_id INT)
BEGIN
    UPDATE Pedidos 
    SET ValorTotal = (
        SELECT COALESCE(SUM(Subtotal), 0) 
        FROM ItensPedido 
        WHERE PedidoId = pedido_id
    )
    WHERE Id = pedido_id;
END //
DELIMITER ;

-- Procedure: Obter Saldo Devedor de um Pedido
DELIMITER //
CREATE PROCEDURE sp_ObterSaldoDevedor(IN pedido_id INT)
BEGIN
    SELECT 
        p.Id,
        p.ValorTotal,
        COALESCE(SUM(pg.ValorPago), 0) AS TotalPago,
        (p.ValorTotal - COALESCE(SUM(pg.ValorPago), 0)) AS SaldoDevedor
    FROM Pedidos p
    LEFT JOIN Pagamentos pg ON p.Id = pg.PedidoId
    WHERE p.Id = pedido_id
    GROUP BY p.Id, p.ValorTotal;
END //
DELIMITER ;

-- ============================================
-- Triggers
-- ============================================

-- Trigger: Calcular Subtotal ao Inserir Item
DELIMITER //
CREATE TRIGGER trg_CalcularSubtotal_Insert
BEFORE INSERT ON ItensPedido
FOR EACH ROW
BEGIN
    SET NEW.Subtotal = NEW.Quantidade * NEW.PrecoUnitario;
END //
DELIMITER ;

-- Trigger: Calcular Subtotal ao Atualizar Item
DELIMITER //
CREATE TRIGGER trg_CalcularSubtotal_Update
BEFORE UPDATE ON ItensPedido
FOR EACH ROW
BEGIN
    SET NEW.Subtotal = NEW.Quantidade * NEW.PrecoUnitario;
END //
DELIMITER ;

-- Trigger: Atualizar Total do Pedido após Inserir Item
DELIMITER //
CREATE TRIGGER trg_AtualizarTotalPedido_Insert
AFTER INSERT ON ItensPedido
FOR EACH ROW
BEGIN
    CALL sp_CalcularTotalPedido(NEW.PedidoId);
END //
DELIMITER ;

-- Trigger: Atualizar Total do Pedido após Atualizar Item
DELIMITER //
CREATE TRIGGER trg_AtualizarTotalPedido_Update
AFTER UPDATE ON ItensPedido
FOR EACH ROW
BEGIN
    CALL sp_CalcularTotalPedido(NEW.PedidoId);
END //
DELIMITER ;

-- Trigger: Atualizar Total do Pedido após Deletar Item
DELIMITER //
CREATE TRIGGER trg_AtualizarTotalPedido_Delete
AFTER DELETE ON ItensPedido
FOR EACH ROW
BEGIN
    CALL sp_CalcularTotalPedido(OLD.PedidoId);
END //
DELIMITER ;

-- ============================================
-- Consultas de Exemplo
-- ============================================

-- Ver todos os pedidos com saldo devedor
-- SELECT * FROM vw_PedidosComSaldo;

-- Ver produtos mais vendidos
-- SELECT * FROM vw_ProdutosMaisVendidos;

-- Ver clientes que mais compram
-- SELECT * FROM vw_ClientesTop;

-- Ver relatório de produção
-- SELECT * FROM vw_RelatorioProducao;

-- Ver saldo devedor de um pedido específico
-- CALL sp_ObterSaldoDevedor(1);
