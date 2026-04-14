import React, { useState, useCallback, useRef } from 'react';
import './CustomSelect.css';

export interface SelectOption {
  value: string;
  label: string;
}

interface CustomSelectProps {
  id?: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export const CustomSelect: React.FC<CustomSelectProps> = ({
  id,
  value,
  options,
  onChange,
  placeholder = 'Wybierz opcję',
  className = '',
  disabled = false,
}) => {
  const [dropdownVisible, setDropdownVisible] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const currentSelection = options.find(opt => opt.value === value);

  const toggleDropdown = useCallback(() => {
    if (!disabled) {
      setDropdownVisible(prev => !prev);
    }
  }, [disabled]);

  const selectOption = useCallback((selectedValue: string) => {
    onChange(selectedValue);
    setDropdownVisible(false);
  }, [onChange]);

  const closeOnOutsideClick = useCallback((event: Event) => {
    const target = event.target as Node;
    if (containerRef.current && !containerRef.current.contains(target)) {
      setDropdownVisible(false);
    }
  }, []);

  React.useEffect(() => {
    if (dropdownVisible) {
      document.addEventListener('click', closeOnOutsideClick, true);
    } else {
      document.removeEventListener('click', closeOnOutsideClick, true);
    }

    return () => {
      document.removeEventListener('click', closeOnOutsideClick, true);
    };
  }, [dropdownVisible, closeOnOutsideClick]);

  return (
    <div
      ref={containerRef}
      className={`custom-select ${className} ${disabled ? 'disabled' : ''}`}
      id={id}
    >
      <button
        type="button"
        className={`custom-select-button ${dropdownVisible ? 'expanded' : ''}`}
        onClick={toggleDropdown}
        disabled={disabled}
        aria-expanded={dropdownVisible}
        aria-haspopup="listbox"
      >
        <span className={currentSelection ? 'selected-label' : 'placeholder-label'}>
          {currentSelection ? currentSelection.label : placeholder}
        </span>
        <span className="dropdown-indicator">▾</span>
      </button>
      {dropdownVisible && (
        <ul className="custom-select-list" role="listbox">
          {options.map(option => (
            <li
              key={option.value}
              className={`custom-select-item ${option.value === value ? 'active' : ''}`}
              onClick={() => selectOption(option.value)}
              role="option"
              aria-selected={option.value === value}
            >
              {option.label}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};