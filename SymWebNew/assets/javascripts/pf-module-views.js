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

    function usesAlignedModuleViews(page) {
        var module = page.getAttribute('data-module');
        return module === 'WWF' || module === 'WPPF';
    }

    function menuText(link) {
        var label = link.querySelector('p');
        if (!label) return '';
        var copy = label.cloneNode(true);
        Array.prototype.forEach.call(copy.querySelectorAll('span, i'), function (child) {
            child.parentNode.removeChild(child);
        });
        return copy.textContent.trim();
    }

    function moduleBreadcrumb(page, text) {
        var module = page.getAttribute('data-module');
        var controller = page.getAttribute('data-controller') || '';
        var action = page.getAttribute('data-action') || '';
        var moduleLink = document.querySelector('.nav-sidebar [data-menu-id="module-' + module + '"]');
        var branch = moduleLink ? moduleLink.closest('li.nav-item') : null;
        var current = branch ? branch.querySelector('[data-current-menu="true"]') : null;
        var items = [];
        if (current) {
            var parent = current.closest('li.nav-item');
            while (parent) {
                var link = parent.querySelector('a.nav-link');
                if (link) items.unshift({ text: menuText(link), href: link.getAttribute('href') });
                if (parent === branch) break;
                parent = parent.parentNode.closest('li.nav-item');
            }
        }
        if (!items.length) {
            items.push({ text: module, href: '/' + module + '/Home/Index' });
            if (controller.toLowerCase() !== 'home') {
                items.push({ text: text.split('>')[0].replace(/\s+List\s*$/, '').trim() || controller, href: window.location.pathname });
            }
        }
        // Preserve the form's Create/Edit/etc. suffix after the full menu hierarchy.
        var steps = text.split('>').slice(1).map(function (step) { return step.trim(); }).filter(Boolean);
        if (!steps.length && /^(create|edit)$/i.test(action)) steps.push(action);
        steps.forEach(function (step) { items.push({ text: step, href: '#' }); });
        return items;
    }

    function pageHeader(page) {
        if (page.getAttribute('data-pf-heading')) return;
        var existing = page.querySelector('.th_thinbox_top');
        var label = page.querySelector('.erp-breadcrumb label, .headrow > label.pull-right');
        var heading = page.querySelector('.th_thinbox_top p, .th_thinbox p, .erp-card-header p, h1, h2');
        var text = (label ? label.textContent : (heading ? heading.textContent : '')).trim()
            || page.getAttribute('data-controller') || page.getAttribute('data-module');
        if (label && usesAlignedModuleViews(page)) label.classList.add('pf-module-page-label');
        var title = text.split('>')[0].replace(/\s+List\s*$/, '').trim();
        var card = element('div', 'card pf-module-page-header');
        var header = element('div', 'card-header p-3');
        var content = element('div', 'th_thinbox_top');
        var name = element('p', '');
        name.textContent = title;
        var breadcrumb = element('div', 'breadcrumb-container');
        var list = element('ol', 'breadcrumb');
        list.id = 'dynamicBreadcrumb';
        list.moduleBreadcrumbItems = moduleBreadcrumb(page, text);
        breadcrumb.appendChild(list);
        content.appendChild(name);
        content.appendChild(breadcrumb);
        header.appendChild(content);
        card.appendChild(header);
        // Keep the new heading outside the form so the original form structure stays intact.
        if (existing) {
            var oldBreadcrumb = existing.querySelector('.breadcrumb-container');
            if (oldBreadcrumb) existing.replaceChild(breadcrumb, oldBreadcrumb);
            else existing.appendChild(breadcrumb);
        } else {
            page.insertBefore(card, page.firstChild);
        }
        page.setAttribute('data-pf-heading', 'true');
        if (typeof window.renderDynamicBreadcrumb === 'function') {
            window.renderDynamicBreadcrumb();
        }
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
        if (page && usesAlignedModuleViews(page)) {
            var ancestor = wrapper.parentNode;
            while (ancestor && ancestor !== page) {
                if (ancestor.classList.contains('row')) ancestor.classList.add('pf-module-table-row');
                if (ancestor.id === 'container' || ancestor.id === 'demo') ancestor.classList.add('pf-module-table-container');
                ancestor = ancestor.parentNode;
            }
        }
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

    function moduleAlignment(page) {
        if (!usesAlignedModuleViews(page)) return;
        document.body.classList.add('pf-module-alignment');
        Array.prototype.forEach.call(page.querySelectorAll('.thinbox'), function (panel) {
            Array.prototype.forEach.call(panel.children, function (child) {
                if (child.matches('.th_thinbox, .headrow, .dataTables_wrapper, script, style, table')) return;
                if (!child.querySelector('table.display') && child.querySelector('.row, .form-control, .selectDropdown')) {
                    child.classList.add('pf-module-form-content');
                }
            });
        });
        Array.prototype.forEach.call(page.querySelectorAll('.row'), function (row) {
            if (Array.prototype.some.call(row.children, function (child) {
                return child.matches('button, a[class*="sym-btn"]');
            })) row.classList.add('pf-module-action-row');
        });
    }

    function enhance() {
        var page = document.querySelector('.pf-module-view');
        if (!page) return;
        document.body.classList.add('pf-module-theme');
        moduleAlignment(page);
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
