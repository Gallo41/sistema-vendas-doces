// JavaScript para o Sistema de Vendas de Doces

// Adiciona animação suave ao scroll
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth'
            });
        }
    });
});

// Confirmação antes de excluir
document.querySelectorAll('form[action*="Delete"]').forEach(form => {
    form.addEventListener('submit', function (e) {
        if (!confirm('Tem certeza que deseja excluir este item?')) {
            e.preventDefault();
        }
    });
});

// Formatação automática de valores monetários
document.querySelectorAll('input[type="number"][step="0.01"]').forEach(input => {
    input.addEventListener('blur', function () {
        if (this.value) {
            this.value = parseFloat(this.value).toFixed(2);
        }
    });
});

console.log('🍰 Sistema de Vendas de Doces carregado com sucesso!');
