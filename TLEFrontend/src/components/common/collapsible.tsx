import { ReactNode, useState } from 'react';
import styled from 'styled-components';

const Container = styled.div`
  margin: 8rem 0;
`;

const Header = styled.div<{isOpen: boolean}>`
  display: flex;
  align-items: center;
  padding: 12rem 12rem;  /* More padding */
  cursor: pointer;
  background: rgba(100, 180, 255, 0.12);  /* Main Menu Blue, subtle */
  border: 1rem solid rgba(100, 180, 255, 0.25);  /* Light border */
  border-radius: 6rem;  /* Larger radius */
  transition: all 0.2s ease;
  user-select: none;
  
  &:hover {
    background: rgba(100, 180, 255, 0.18);  /* Lighter on hover */
    border-color: rgba(100, 180, 255, 0.35);
  }
`;

const Title = styled.div`
  flex-grow: 1;
  font-weight: 600;  /* Bolder */
  font-size: 15rem;  /* Slightly larger */
  color: var(--textColor);
`;

const Arrow = styled.div<{isOpen: boolean}>`
  margin-left: 8rem;
  transition: transform 0.2s ease;
  transform: ${props => props.isOpen ? 'rotate(90deg)' : 'rotate(0deg)'};
  color: rgba(100, 180, 255, 1);  /* Main Menu Blue */
  font-size: 16rem;  /* Larger */
  font-weight: bold;  /* Bolder */
`;

const Content = styled.div<{isOpen: boolean}>`
  max-height: ${props => props.isOpen ? '5000rem' : '0'};
  overflow: hidden;
  transition: max-height 0.3s ease;
`;

const ContentInner = styled.div`
  padding-top: 8rem;
`;

interface CollapsibleProps {
  title: string;
  children: ReactNode;
  defaultOpen?: boolean;
}

export default function Collapsible({ title, children, defaultOpen = true }: CollapsibleProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <Container>
      <Header isOpen={isOpen} onClick={() => setIsOpen(!isOpen)}>
        <Title>{title}</Title>
        <Arrow isOpen={isOpen}>&#9658;</Arrow>
      </Header>
      <Content isOpen={isOpen}>
        <ContentInner>
          {children}
        </ContentInner>
      </Content>
    </Container>
  );
}
