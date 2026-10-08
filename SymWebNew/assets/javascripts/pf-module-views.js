(function () {
    'use strict';

    function element(tag, className) {
        var node = document.createElement(tag);
        node.className = className;
        return node;
    }

    function move(node, destination) {
        if (node && node.parentNode !== destination) destination.appendChild(node);
    }

    function pageHeader(page) {
        if (page.querySelector('.th_thinbox_top') || page.getAttribute('data-pf-heading')) return;
        var label = page.querySelector('.erp-breadcrumb label, .headrow > label.pull-right');
        var heading = page.querySelector('.th_thinbox p, .erp-card-header p');
        var text = label ? label.textContent : (heading ? heading.textContent : '');
        if (!text.trim()) return;
        var title = text.split('>')[0].replace(/\s+List\s*$/, '').trim();
        var card = element('div', 'card pf-module-page-header');
        var header = element('div', 'card-header p-3');
        var content = element('div', 'th_thinbox_top');
        var name = element('p', '');
        name.textContent = title;
        var breadcrumb = element('div', 'breadcrumb-container');
        var list = element('ol', 'breadcrumb');
        var item = element('li', 'breadcrumb-item active');
        item.textContent = text.trim();
        list.appendChild(item);
        breadcrumb.appendChild(list);
        content.appendChild(name);
        content.appendChild(breadcrumb);
        header.appendChild(content);
        card.appendChild(header);
        // Keep the new heading outside the form so the original form structure stays intact.
        page.insertBefore(card, page.firstChild);
        page.setAttribute('data-pf-heading', 'true');
    }

    function tableControls(wrapper) {
        var table = wrapper.querySelector('table.display');
        if (!table) return;
        var top = wrapper.querySelector('.pf-module-table-top');
        if (!top) {
            top = element('div', 'custom-top pf-module-table-top');
            top.appendChild(element('div', 'pf-module-actions'));
            top.appendChild(element('div', 'pf-module-search table-search'));
            wrapper.insertBefore(top, wrapper.firstChild);
        }
        var footer = wrapper.querySelector('.pf-module-table-footer');
        if (!footer) {
            footer = element('div', 'custom-footer pf-module-table-footer');
            footer.appendChild(element('div', 'pf-module-length'));
            footer.appendChild(element('div', 'pf-module-info'));
            footer.appendChild(element('div', 'pf-module-pagination'));
            wrapper.appendChild(footer);
        }
        move(wrapper.querySelector('.dataTables_filter'), top.querySelector('.pf-module-search'));
        move(wrapper.querySelector('.dataTables_length'), footer.querySelector('.pf-module-length'));
        move(wrapper.querySelector('.dataTables_info'), footer.querySelector('.pf-module-info'));
        move(wrapper.querySelector('.dataTables_paginate'), footer.querySelector('.pf-module-pagination'));
        var search = top.querySelector('input');
        if (search && !search.placeholder) search.placeholder = 'Search...';

        // Only move index actions when neither the table nor its toolbar belongs to a form.
        var page = table.closest('.pf-module-view');
        if (page && table.id === 'myDataTable' && !table.closest('form')) {
            var bar = page.querySelector('.erp-action-bar, .headrow');
            if (bar && !bar.closest('form') && !bar.getAttribute('data-pf-actions')) {
                var actions = bar.querySelector('.erp-action-btns') || bar;
                Array.prototype.slice.call(actions.children).forEach(function (child) {
                    if (child.matches('button, a[class*="sym-btn"]')) move(child, top.firstChild);
                });
                bar.setAttribute('data-pf-actions', 'true');
                if (!bar.querySelector('button, a, input, select, textarea')) {
                    bar.classList.add('pf-module-actions-only');
                }
            }
        }
        if (!table.closest('.dataTables_scroll') && !table.parentNode.classList.contains('pf-module-table-scroll')) {
            var scroll = element('div', 'pf-module-table-scroll');
            table.parentNode.insertBefore(scroll, table);
            scroll.appendChild(table);
        }
        Array.prototype.forEach.call(wrapper.querySelectorAll('.ui-widget-header'), function (header) {
            if (!header.children.length && !header.textContent.trim()) header.style.display = 'none';
        });
    }

    function enhance() {
        var page = document.querySelector('.pf-module-view');
        if (!page) return;
        document.body.classList.add('pf-module-theme');
        pageHeader(page);
        Array.prototype.forEach.call(document.querySelectorAll('.dataTables_wrapper'), tableControls);
    }

    function start() {
        if (!document.querySelector('.pf-module-view')) return;
        enhance();
        var scheduled = false;
        var observer = new MutationObserver(function (changes) {
            var hasNewContent = changes.some(function (change) { return change.addedNodes.length; });
            if (!hasNewContent || scheduled) return;
            scheduled = true;
            window.requestAnimationFrame(function () { scheduled = false; enhance(); });
        });
        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
}());
